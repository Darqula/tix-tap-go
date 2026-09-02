using Microsoft.EntityFrameworkCore;

using TixTapGo.Shared.Persistence.DAL;
using TixTapGo.Shared.Persistence.Entities;

using Xunit;

namespace TixTapGo.Shared.Persistence.Tests;

/// <summary>
/// Integration tests for <see cref="HierarchicalSoftDeleteCommand"/> against a real Postgres
/// instance (via Testcontainers). Each test uses its own schema (see
/// <see cref="TestDbContextFactory"/>) and re-opens fresh <see cref="TestDbContext"/> instances
/// around the delete under test, so assertions read back real persisted state rather than
/// relying on the change tracker of the context that performed the delete.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class HierarchicalSoftDeleteCommandTests(PostgresCollectionFixture fixture) : IAsyncLifetime
{
    private TestDbContextFactory _contextFactory = null!;
    private TestDbContext _context = null!;

    public async Task InitializeAsync()
    {
        _contextFactory = new TestDbContextFactory(fixture.ConnectionString);
        _context = await _contextFactory.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _contextFactory.DisposeAsync();
    }

    [Fact]
    public async Task Cascade_SoftDeletesDirectChild_WhenNavigationWasNotPreloaded()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme"
        };
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Company = company,
            Name = "HQ"
        };
        _context.Companies.Add(company);
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCompany = await deleteContext.Companies.SingleAsync(c => c.Id == company.Id);
        var entry = deleteContext
            .Entry<EntityBase>(trackedCompany); // The Branches navigation is deliberately not loaded here.

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedCompany = await verifyContext.Companies.IgnoreQueryFilters().SingleAsync(c => c.Id == company.Id);
        var persistedBranch = await verifyContext.Branches.IgnoreQueryFilters().SingleAsync(b => b.Id == branch.Id);

        Assert.True(persistedCompany.IsDeleted);
        Assert.True(persistedBranch.IsDeleted);
    }

    [Fact]
    public async Task Cascade_SkipsAlreadySoftDeletedSibling_AndNeverReachesItsOwnDescendants()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme"
        };
        var branchLive = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Company = company,
            Name = "Live"
        };
        var branchAlreadyDeleted = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Company = company,
            Name = "Already gone"
        };
        _context.AddRange(company, branchLive, branchAlreadyDeleted);
        await _context.SaveChangesAsync();

        DateTimeOffset updatedAtAfterFirstDelete;
        await using (var preDeleteContext = await _contextFactory.CreateAsync())
        {
            var trackedBranch = await preDeleteContext.Branches.SingleAsync(b => b.Id == branchAlreadyDeleted.Id);
            preDeleteContext.Remove(trackedBranch); // SharedDbContext converts this to a soft delete automatically.
            await preDeleteContext.SaveChangesAsync();
        }

        // Added after its parent branch was already soft-deleted, "orphaned from the start" shape
        // that the later Company-level delete is expected to leave untouched.
        // FK scalar only, no .Branch navigation reference: branchAlreadyDeleted is an entity from
        // a different DbContext instance (_context), and setting the navigation would make EF's
        // graph-tracking on Add() try to re-insert it (and Company, transitively) as new rows.
        var orphanedEmployee = new Employee
        {
            Id = Guid.NewGuid(),
            BranchId = branchAlreadyDeleted.Id,
            Name = "Bob"
        };
        await using (var addEmployeeContext = await _contextFactory.CreateAsync())
        {
            addEmployeeContext.Employees.Add(orphanedEmployee);
            await addEmployeeContext.SaveChangesAsync();
        }
        
        await using (var rereadContext = await _contextFactory.CreateAsync())
        {
            var reread = await rereadContext.Branches.IgnoreQueryFilters()
                .SingleAsync(b => b.Id == branchAlreadyDeleted.Id);
            updatedAtAfterFirstDelete = reread.UpdatedAt;
        }

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCompany = await deleteContext.Companies.SingleAsync(c => c.Id == company.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCompany);

        // Deleting Company: branchAlreadyDeleted is already excluded by the global !IsDeleted
        // filter when the command loads Company.Branches, so the traversal should stop in that
        // direction - it must not touch branchAlreadyDeleted again, and must not reach
        // orphanedEmployee through it either.
        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedBranchLive =
            await verifyContext.Branches.IgnoreQueryFilters().SingleAsync(b => b.Id == branchLive.Id);
        var persistedBranchAlreadyDeleted = await verifyContext.Branches.IgnoreQueryFilters()
            .SingleAsync(b => b.Id == branchAlreadyDeleted.Id);
        var persistedOrphanedEmployee = await verifyContext.Employees.SingleAsync(e => e.Id == orphanedEmployee.Id);

        Assert.True(persistedBranchLive.IsDeleted); // the live sibling was cascaded normally
        Assert.True(persistedBranchAlreadyDeleted.IsDeleted); // unchanged, was already true
        Assert.Equal(updatedAtAfterFirstDelete, persistedBranchAlreadyDeleted.UpdatedAt); // never touched a second time
        Assert.False(persistedOrphanedEmployee
            .IsDeleted); // never reached - traversal stopped at its already-deleted parent
    }

    /// <summary>
    /// Distinct from <see cref="Cascade_SkipsAlreadySoftDeletedSibling_AndNeverReachesItsOwnDescendants"/>,
    /// which exercises the guard that skips an already-deleted dependent reached mid-cascade.
    /// This calls <see cref="HierarchicalSoftDeleteCommand.ExecuteAsync"/> directly on a top-level
    /// entry that is already soft-deleted, exercising <c>ExecuteAsync</c>'s own early-return guard.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NoOps_WhenCalledDirectlyOnAnAlreadySoftDeletedEntry()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Soft-delete it through the normal flow first, independently of the command invocation under test.
        await using (var preDeleteContext = await _contextFactory.CreateAsync())
        {
            var trackedCategory = await preDeleteContext.Categories.SingleAsync(c => c.Id == category.Id);
            preDeleteContext.Remove(trackedCategory); // SharedDbContext converts this to a soft delete automatically.
            await preDeleteContext.SaveChangesAsync();
        }
        
        DateTimeOffset updatedAtAfterFirstDelete;
        await using (var rereadContext = await _contextFactory.CreateAsync())
        {
            var reread = await rereadContext.Categories.IgnoreQueryFilters()
                .SingleAsync(c => c.Id == category.Id);
            updatedAtAfterFirstDelete = reread.UpdatedAt;
        }

        // Call the command directly a second time on the already-deleted entry.
        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCategoryAgain = await deleteContext.Categories.IgnoreQueryFilters()
            .SingleAsync(c => c.Id == category.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCategoryAgain);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedCategory = await verifyContext.Categories.IgnoreQueryFilters()
            .SingleAsync(c => c.Id == category.Id);

        Assert.Equal(updatedAtAfterFirstDelete, persistedCategory.UpdatedAt); // never touched a second time
    }

    [Fact]
    public async Task Cascade_RecursesTwoLevelsDeep_ToGrandchildren()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme"
        };
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Company = company,
            Name = "HQ"
        };
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            BranchId = branch.Id,
            Branch = branch,
            Name = "Alice"
        };
        _context.AddRange(company, branch, employee);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCompany = await deleteContext.Companies.SingleAsync(c => c.Id == company.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCompany);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedEmployee =
            await verifyContext.Employees.IgnoreQueryFilters().SingleAsync(e => e.Id == employee.Id);

        Assert.True(persistedEmployee.IsDeleted);
    }

    [Fact]
    public async Task Cascade_HardDeletes_NonEntityBaseDependents()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme"
        };
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Company = company,
            Name = "HQ"
        };
        var note = new AuditNote
        {
            Id = Guid.NewGuid(),
            BranchId = branch.Id,
            Branch = branch,
            Text = "opened"
        };
        _context.AddRange(company, branch, note);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCompany = await deleteContext.Companies.SingleAsync(c => c.Id == company.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCompany);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        // AuditNote has no IsDeleted column at all - a soft-delete flag can't verify anything here.
        // The only meaningful assertion is that the row is physically gone.
        await using var verifyContext = await _contextFactory.CreateAsync();
        var stillExists = await verifyContext.AuditNotes.AnyAsync(a => a.Id == note.Id);

        Assert.False(stillExists);
    }

    [Fact]
    public async Task Cascade_SoftDeletesAllChildrenInCollection_NotJustFirst()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme"
        };
        var branches = Enumerable.Range(1, 3)
            .Select(i => new Branch
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Company = company,
                Name = $"Branch {i}"
            })
            .ToList();
        _context.Companies.Add(company);
        _context.Branches.AddRange(branches);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCompany = await deleteContext.Companies.SingleAsync(c => c.Id == company.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCompany);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedBranches = await verifyContext.Branches.IgnoreQueryFilters()
            .Where(b => b.CompanyId == company.Id)
            .ToListAsync();

        Assert.Equal(3, persistedBranches.Count);
        Assert.All(persistedBranches, b => Assert.True(b.IsDeleted));
    }

    [Fact]
    public async Task Cascade_CompletesCleanly_WhenCollectionIsEmpty()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme"
        };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCompany = await deleteContext.Companies.SingleAsync(c => c.Id == company.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCompany);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedCompany = await verifyContext.Companies.IgnoreQueryFilters().SingleAsync(c => c.Id == company.Id);

        Assert.True(persistedCompany.IsDeleted);
    }

    [Fact]
    public async Task Cascade_ProcessesEveryReferencingForeignKey_OnSamePrincipal()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme"
        };
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Company = company,
            Name = "HQ"
        };
        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Company = company,
            Reference = "C-001"
        };
        _context.AddRange(company, branch, contract);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCompany = await deleteContext.Companies.SingleAsync(c => c.Id == company.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCompany);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedBranch = await verifyContext.Branches.IgnoreQueryFilters().SingleAsync(b => b.Id == branch.Id);
        var persistedContract =
            await verifyContext.Contracts.IgnoreQueryFilters().SingleAsync(c => c.Id == contract.Id);

        Assert.True(persistedBranch.IsDeleted);
        Assert.True(persistedContract.IsDeleted);
    }

    [Fact]
    public async Task Cascade_AppliesSetNull_AtANestedRecursionLevel()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme"
        };
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Company = company,
            Name = "HQ"
        };
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            BranchId = branch.Id,
            Branch = branch,
            Name = "Alice"
        };
        var locker = new Locker
        {
            Id = Guid.NewGuid(),
            Code = "L-42",
            AssignedEmployeeId = employee.Id,
            AssignedEmployee = employee
        };
        _context.AddRange(company, branch, employee, locker);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCompany = await deleteContext.Companies.SingleAsync(c => c.Id == company.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCompany);

        // Company -> Branch -> Employee is already a two-level Cascade recursion. The SetNull
        // on Locker only fires if the recursive call correctly re-walks Employee's own
        // referencing foreign keys, not just the top-level entry's.
        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedEmployee =
            await verifyContext.Employees.IgnoreQueryFilters().SingleAsync(e => e.Id == employee.Id);
        var persistedLocker = await verifyContext.Lockers.SingleAsync(l => l.Id == locker.Id);

        Assert.True(persistedEmployee.IsDeleted);
        Assert.Null(persistedLocker.AssignedEmployeeId);
        Assert.False(persistedLocker.IsDeleted);
    }

    [Fact]
    public async Task Diamond_SameDependentReachedViaTwoCascadePaths_DoesNotThrowOrHang()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Launch"
        };
        var workstreamA = new WorkstreamA
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Project = project
        };
        var workstreamB = new WorkstreamB
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Project = project
        };
        var note = new SharedNote
        {
            Id = Guid.NewGuid(),
            WorkstreamAId = workstreamA.Id,
            WorkstreamA = workstreamA,
            WorkstreamBId = workstreamB.Id,
            WorkstreamB = workstreamB,
            Text = "shared across both workstreams"
        };
        _context.AddRange(project, workstreamA, workstreamB, note);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedProject = await deleteContext.Projects.SingleAsync(p => p.Id == project.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedProject);

        // note is reachable via both Project -> WorkstreamA -> SharedNote and
        // Project -> WorkstreamB -> SharedNote. The already-processed guard in
        // CascadeDeleteEntityObjectAsync only checks EntityState.Deleted, but soft-deleted
        // entries end up Modified, so the second path re-enters processing for the same entry.
        // This test asserts the operation completes without hanging on that cycle-shaped
        // re-entry and ends in a correct, idempotent state, rather than asserting how many
        // times the entry gets visited.
        var workTask = new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        var winner = await Task.WhenAny(workTask, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(workTask, winner);
        await workTask; // re-observe so any exception actually surfaces in this test

        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedNote = await verifyContext.SharedNotes.IgnoreQueryFilters().SingleAsync(n => n.Id == note.Id);

        Assert.True(persistedNote.IsDeleted);
    }

    [Fact]
    public async Task SetNull_CompletesCleanly_WhenNoDependentsExist()
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Central"
        };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedWarehouse = await deleteContext.Warehouses.SingleAsync(w => w.Id == warehouse.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedWarehouse);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedWarehouse =
            await verifyContext.Warehouses.IgnoreQueryFilters().SingleAsync(w => w.Id == warehouse.Id);

        Assert.True(persistedWarehouse.IsDeleted);
    }

    [Fact]
    public async Task SetNull_ClearsOptionalForeignKey_ButDoesNotDeleteDependent()
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Central"
        };
        var store = new StoreLocation
        {
            Id = Guid.NewGuid(),
            Name = "Downtown",
            PreferredWarehouseId = warehouse.Id,
            PreferredWarehouse = warehouse
        };
        _context.AddRange(warehouse, store);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedWarehouse = await deleteContext.Warehouses.SingleAsync(w => w.Id == warehouse.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedWarehouse);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedStore = await verifyContext.StoreLocations.SingleAsync(s => s.Id == store.Id);

        Assert.Null(persistedStore.PreferredWarehouseId);
        Assert.False(persistedStore.IsDeleted); // severing the relationship must not delete the dependent
    }

    [Fact]
    public async Task SetNull_SkipsAlreadySoftDeletedDependent_LeavingItsForeignKeyUntouched()
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Central"
        };
        var storeLive = new StoreLocation
        {
            Id = Guid.NewGuid(),
            Name = "Live",
            PreferredWarehouseId = warehouse.Id,
            PreferredWarehouse = warehouse
        };
        var storeAlreadyDeleted = new StoreLocation
        {
            Id = Guid.NewGuid(),
            Name = "Already gone",
            PreferredWarehouseId = warehouse.Id,
            PreferredWarehouse = warehouse
        };
        _context.AddRange(warehouse, storeLive, storeAlreadyDeleted);
        await _context.SaveChangesAsync();

        DateTimeOffset updatedAtAfterFirstDelete;
        await using (var preDeleteContext = await _contextFactory.CreateAsync())
        {
            var trackedStore = await preDeleteContext.StoreLocations.SingleAsync(s => s.Id == storeAlreadyDeleted.Id);
            preDeleteContext.Remove(trackedStore); // SharedDbContext converts this to a soft delete automatically.
            await preDeleteContext.SaveChangesAsync();
        }

        await using (var rereadContext = await _contextFactory.CreateAsync())
        {
            var reread = await rereadContext.StoreLocations.IgnoreQueryFilters()
                .SingleAsync(s => s.Id == storeAlreadyDeleted.Id);
            updatedAtAfterFirstDelete = reread.UpdatedAt;
        }

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedWarehouse = await deleteContext.Warehouses.SingleAsync(w => w.Id == warehouse.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedWarehouse);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedStoreLive = await verifyContext.StoreLocations.SingleAsync(s => s.Id == storeLive.Id);
        var persistedStoreAlreadyDeleted = await verifyContext.StoreLocations.IgnoreQueryFilters()
            .SingleAsync(s => s.Id == storeAlreadyDeleted.Id);

        Assert.Null(persistedStoreLive.PreferredWarehouseId); // the live sibling was nulled normally
        Assert.Equal(warehouse.Id,
            persistedStoreAlreadyDeleted.PreferredWarehouseId); // untouched - still points at the deleted warehouse
        Assert.Equal(updatedAtAfterFirstDelete, persistedStoreAlreadyDeleted.UpdatedAt); // never touched a second time
    }

    /// <summary>
    /// Verifies that <see cref="HierarchicalSoftDeleteCommand"/>'s SetNull path stamps
    /// <c>UpdatedAt</c> on the dependent it nulls out, in addition to clearing the foreign key.
    /// </summary>
    [Fact]
    public async Task SetNull_StampsUpdatedAtOnDependent_ViaProcessTrackedEntitiesSafetyNet()
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = "Central"
        };
        var store = new StoreLocation
        {
            Id = Guid.NewGuid(),
            Name = "Downtown",
            PreferredWarehouseId = warehouse.Id,
            PreferredWarehouse = warehouse
        };
        _context.AddRange(warehouse, store);
        await _context.SaveChangesAsync();
        var beforeDelete = DateTimeOffset.UtcNow;

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedWarehouse = await deleteContext.Warehouses.SingleAsync(w => w.Id == warehouse.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedWarehouse);
        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedStore = await verifyContext.StoreLocations.SingleAsync(s => s.Id == store.Id);

        Assert.True(persistedStore.UpdatedAt >= beforeDelete);
    }

    [Fact]
    public async Task Restrict_Throws_WhenLiveDependentsExist()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics"
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category,
            Name = "Radio"
        };
        _context.AddRange(category, product);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCategory = await deleteContext.Categories.SingleAsync(c => c.Id == category.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCategory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry));
    }

    [Fact]
    public async Task Restrict_Allows_WhenNoDependentsExist()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCategory = await deleteContext.Categories.SingleAsync(c => c.Id == category.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCategory);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedCategory =
            await verifyContext.Categories.IgnoreQueryFilters().SingleAsync(c => c.Id == category.Id);

        Assert.True(persistedCategory.IsDeleted);
    }

    [Fact]
    public async Task Restrict_Allows_WhenOnlyDependentIsAlreadySoftDeleted()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics"
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category,
            Name = "Radio"
        };
        _context.AddRange(category, product);
        await _context.SaveChangesAsync();

        // Soft-delete the product first, independently of the command under test.
        await using (var preDeleteContext = await _contextFactory.CreateAsync())
        {
            var trackedProduct = await preDeleteContext.Products.SingleAsync(p => p.Id == product.Id);
            preDeleteContext.Remove(trackedProduct); // SharedDbContext converts this to a soft delete automatically.
            await preDeleteContext.SaveChangesAsync();
        }

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedCategory = await deleteContext.Categories.SingleAsync(c => c.Id == category.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedCategory);

        // Must not throw: the command's own Load() (or reflection-based lookup) respects the
        // global soft-delete query filter, so the already-deleted product no longer counts as a
        // live dependent.
        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedCategory =
            await verifyContext.Categories.IgnoreQueryFilters().SingleAsync(c => c.Id == category.Id);

        Assert.True(persistedCategory.IsDeleted);
    }

    /// <summary>
    /// A relationship's DeleteBehavior must be honored even when only the dependent side exposes
    /// a navigation property. <see cref="Playlist"/> deliberately has no collection navigation to
    /// <see cref="Track"/> - <see cref="HierarchicalSoftDeleteCommand"/> falls back to a
    /// reflection-based lookup keyed by the foreign key value itself
    /// (<c>GetDependantsByReflectionAsync</c>) when no principal-side navigation exists, so it
    /// should still discover and cascade to Tracks.
    /// </summary>
    [Fact]
    public async Task MissingBackNavigation_StillCascades_ToDependentSide()
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            Name = "Roadtrip"
        };
        var track = new Track
        {
            Id = Guid.NewGuid(),
            PlaylistId = playlist.Id,
            Playlist = playlist,
            Title = "Song"
        };
        _context.AddRange(playlist, track);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedPlaylist = await deleteContext.Playlists.SingleAsync(p => p.Id == playlist.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedPlaylist);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedPlaylist =
            await verifyContext.Playlists.IgnoreQueryFilters().SingleAsync(p => p.Id == playlist.Id);
        var persistedTrack = await verifyContext.Tracks.IgnoreQueryFilters().SingleAsync(t => t.Id == track.Id);

        Assert.True(persistedPlaylist.IsDeleted);
        Assert.True(persistedTrack.IsDeleted);
    }

    /// <summary>
    /// Every other cluster's principal-side navigation is a collection (or absent entirely,
    /// forcing the reflection fallback). Vehicle-&gt;Engine is a genuine 1:1: the principal
    /// (Vehicle) exposes a single Reference navigation, exercising the <c>!isCollection</c> branch
    /// of <c>GetDependantsByNavigationAsync</c>, which none of those other tests reach.
    /// </summary>
    [Fact]
    public async Task Cascade_SoftDeletesDependent_ViaPrincipalSideReferenceNavigation()
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Vin = "VIN-1"
        };
        var engine = new Engine
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            SerialNumber = "ENG-1"
        };
        _context.AddRange(vehicle, engine);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedVehicle = await deleteContext.Vehicles.SingleAsync(v => v.Id == vehicle.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedVehicle);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedEngine = await verifyContext.Engines.IgnoreQueryFilters().SingleAsync(e => e.Id == engine.Id);

        Assert.True(persistedEngine.IsDeleted);
    }

    [Fact]
    public async Task Cascade_CompletesCleanly_ViaReferenceNavigation_WhenNoDependentExists()
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Vin = "VIN-2"
        };
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedVehicle = await deleteContext.Vehicles.SingleAsync(v => v.Id == vehicle.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedVehicle);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedVehicle = await verifyContext.Vehicles.IgnoreQueryFilters().SingleAsync(v => v.Id == vehicle.Id);

        Assert.True(persistedVehicle.IsDeleted);
    }

    [Fact]
    public async Task CompositeKey_Cascade_ScopesToBothColumns_ViaNavigation()
    {
        // Control case: Facility exposes a collection navigation to Asset, so this goes through
        // GetDependantsByNavigationAsync (EF's own Load()), which handles composite keys
        // correctly. Two Facilities share a RegionCode but differ on SiteCode - deleting one
        // must not touch the other's Asset.
        var facilityA = new Facility
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            SiteCode = "A1",
            Name = "Facility A"
        };
        var facilityB = new Facility
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            SiteCode = "B2",
            Name = "Facility B"
        };
        var assetA = new Asset
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            SiteCode = "A1",
            Facility = facilityA,
            Tag = "A-1"
        };
        var assetB = new Asset
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            SiteCode = "B2",
            Facility = facilityB,
            Tag = "B-1"
        };
        _context.AddRange(facilityA, facilityB, assetA, assetB);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedFacilityA = await deleteContext.Facilities.SingleAsync(f => f.Id == facilityA.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedFacilityA);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedAssetA = await verifyContext.Assets.IgnoreQueryFilters().SingleAsync(a => a.Id == assetA.Id);
        var persistedAssetB = await verifyContext.Assets.IgnoreQueryFilters().SingleAsync(a => a.Id == assetB.Id);

        Assert.True(persistedAssetA.IsDeleted); // same region and site as the deleted facility
        Assert.False(persistedAssetB.IsDeleted); // same region, different site - must survive
    }

    /// <summary>
    /// Depot has no back-collection to Shipment, so this goes through the reflection-based
    /// fallback (<c>GetDependantsByReflectionAsync</c>/<c>LoadDependentsGeneric</c>) rather than
    /// <c>GetDependantsByNavigationAsync</c>. Two Depots share a RegionCode but differ on
    /// SiteCode - deleting one must scope to both key columns and cascade only to its own
    /// Shipment, not the other Depot's.
    /// </summary>
    [Fact]
    public async Task CompositeKey_Cascade_ScopesToBothColumns_ViaReflectionFallback()
    {
        var depotA = new Depot
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            SiteCode = "A1",
            Name = "Depot A"
        };
        var depotB = new Depot
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            SiteCode = "B2",
            Name = "Depot B"
        };
        var shipmentA = new Shipment
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            SiteCode = "A1",
            Depot = depotA,
            TrackingNumber = "A-1"
        };
        var shipmentB = new Shipment
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            SiteCode = "B2",
            Depot = depotB,
            TrackingNumber = "B-1"
        };
        _context.AddRange(depotA, depotB, shipmentA, shipmentB);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedDepotA = await deleteContext.Depots.SingleAsync(d => d.Id == depotA.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedDepotA);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedShipmentA =
            await verifyContext.Shipments.IgnoreQueryFilters().SingleAsync(s => s.Id == shipmentA.Id);
        var persistedShipmentB =
            await verifyContext.Shipments.IgnoreQueryFilters().SingleAsync(s => s.Id == shipmentB.Id);

        Assert.True(persistedShipmentA.IsDeleted); // same region and site as the deleted depot
        Assert.False(persistedShipmentB.IsDeleted); // same region, different site - must survive
    }

    /// <summary>
    /// Verifies that SetNull clears every column of a composite foreign key, not just the
    /// first, so the dependent is never left with a half-severed, inconsistent reference to
    /// the deleted principal.
    /// </summary>
    [Fact]
    public async Task CompositeKey_SetNull_ClearsBothColumns()
    {
        var district = new District
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            DistrictCode = "D1",
            Name = "District 1"
        };
        var outlet = new Outlet
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            DistrictCode = "D1",
            District = district,
            Name = "Outlet 1"
        };
        _context.AddRange(district, outlet);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedDistrict = await deleteContext.Districts.SingleAsync(d => d.Id == district.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedDistrict);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedOutlet = await verifyContext.Outlets.SingleAsync(o => o.Id == outlet.Id);

        Assert.Null(persistedOutlet.RegionCode);
        Assert.Null(persistedOutlet.DistrictCode);
        Assert.False(persistedOutlet.IsDeleted); // severing the relationship must not delete the dependent
    }

    /// <summary>
    /// None of the Facility/Depot/District composite-key clusters use Restrict - Category/Product,
    /// the only existing Restrict cluster, has a single-column FK. This exercises Restrict's
    /// dependants check against a composite key.
    /// </summary>
    [Fact]
    public async Task CompositeKey_Restrict_Throws_WhenOwnZoneHasLiveMeter()
    {
        var zone = new Zone
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            ZoneCode = "A1",
            Name = "Zone A"
        };
        var meter = new Meter
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            ZoneCode = "A1",
            Zone = zone,
            SerialNumber = "A-1"
        };
        _context.AddRange(zone, meter);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedZone = await deleteContext.Zones.SingleAsync(z => z.Id == zone.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedZone);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry));
    }

    /// <summary>
    /// zoneA has no Meter of its own - only zoneB (same Region, different Zone) does. If Restrict's
    /// composite-key dependants lookup degraded to matching on RegionCode alone, it would
    /// incorrectly find zoneB's Meter and throw for zoneA too.
    /// </summary>
    [Fact]
    public async Task CompositeKey_Restrict_ScopesToBothColumns_AllowsWhenOnlyOtherZoneHasMeter()
    {
        var zoneA = new Zone
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            ZoneCode = "A1",
            Name = "Zone A"
        };
        var zoneB = new Zone
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            ZoneCode = "B2",
            Name = "Zone B"
        };
        var meterB = new Meter
        {
            Id = Guid.NewGuid(),
            RegionCode = "EU",
            ZoneCode = "B2",
            Zone = zoneB,
            SerialNumber = "B-1"
        };
        _context.AddRange(zoneA, zoneB, meterB);
        await _context.SaveChangesAsync();

        await using var deleteContext = await _contextFactory.CreateAsync();
        var trackedZoneA = await deleteContext.Zones.SingleAsync(z => z.Id == zoneA.Id);
        var entry = deleteContext.Entry<EntityBase>(trackedZoneA);

        await new HierarchicalSoftDeleteCommand(deleteContext).ExecuteAsync(entry);
        await deleteContext.SaveChangesAsync();

        await using var verifyContext = await _contextFactory.CreateAsync();
        var persistedZoneA = await verifyContext.Zones.IgnoreQueryFilters().SingleAsync(z => z.Id == zoneA.Id);

        Assert.True(persistedZoneA.IsDeleted); // no live Meter of its own
    }
}

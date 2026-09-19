using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using TixTapGo.Shared.Persistence.DAL;

namespace TixTapGo.OrderService.DAL;

public class OrderDbContext : SharedDbContext
{
    public OrderDbContext(
        DbContextOptions options,
        IEnumerable<ISaveChangesInterceptor> interceptors) : base(options, interceptors)
    {
    }
}

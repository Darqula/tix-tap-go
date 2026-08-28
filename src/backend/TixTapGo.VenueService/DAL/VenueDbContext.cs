using Microsoft.EntityFrameworkCore;

using TixTapGo.Shared.DAL;

namespace TixTapGo.VenueService.DAL;

public class VenueDbContext(DbContextOptions<VenueDbContext> options) : SharedDbContext(options)
{
}

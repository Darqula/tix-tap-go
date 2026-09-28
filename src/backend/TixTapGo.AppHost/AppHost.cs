using TixTapGo.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var infra = builder.AddInfrastructure();
var authParams = builder.AddClientAuthParameters();

var auth = builder.AddAuthService(infra, authParams);
var venues = builder.AddVenueService(infra, authParams, auth);
var events = builder.AddEventService(infra, authParams, auth, venues);
var orders = builder.AddOrderService(infra, authParams, auth);

var gateway = builder.AddGateway(authParams, auth, events, venues, orders);
builder.AddStorefrontMcpService(gateway);

builder.Build().Run();

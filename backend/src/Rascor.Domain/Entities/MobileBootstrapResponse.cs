namespace Rascor.Domain.Entities;

public record MobileBootstrapResponse(
    RemoteConfig Config,
    List<Site> Sites,
    string Etag
);
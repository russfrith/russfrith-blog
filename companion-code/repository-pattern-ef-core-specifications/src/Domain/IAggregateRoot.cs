namespace RepositoryPattern.Domain;

// Marker interface. Repository interfaces constrain T to this, so only
// aggregate roots (Order) can ever get a repository, not their children (OrderLine).
public interface IAggregateRoot;

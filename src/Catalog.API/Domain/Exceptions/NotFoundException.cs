namespace Catalog.API.Domain.Exceptions;

public class NotFoundException : Exception {
	public NotFoundException(string entityName, Guid id)
		: base($"{entityName} with ID {id} not found") { }
}

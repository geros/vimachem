namespace Party.API.Domain.Exceptions;

public class NotFoundException : Exception {
	public NotFoundException(string entity, Guid id)
		: base($"{entity} with ID {id} was not found") { }
}

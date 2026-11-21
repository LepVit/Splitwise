namespace Splitwise.Core.Exceptions;

/// <summary>Базовий виняток бізнес-логіки. Тип винятку визначає HTTP-статус у GlobalExceptionHandler.</summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>Ресурс не знайдено → 404.</summary>
public abstract class NotFoundException : DomainException
{
    protected NotFoundException(string entity, object id)
        : base($"{entity} with ID {id} was not found.") { }

    protected NotFoundException(string message) : base(message) { }
}

/// <summary>Порушено бізнес-правило, що конфліктує зі станом даних → 409.</summary>
public abstract class ConflictException(string message) : DomainException(message);

/// <summary>Вхідні дані не пройшли валідацію → 400.</summary>
public class ValidationException(IDictionary<string, string[]> errors)
    : DomainException("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

public class GroupNotFoundException(int groupId) : NotFoundException("Group", groupId);

public class ExpenseNotFoundException(int expenseId) : NotFoundException("Expense", expenseId);

public class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(int userId) : base("User", userId) { }

    public UserNotFoundException(string email) : base($"User with email '{email}' was not found.") { }
}

public class UserNotInGroupException(int userId, int groupId)
    : ConflictException($"User with ID {userId} is not a member of group {groupId}.");

public class UserAlreadyInGroupException(int userId, int groupId)
    : ConflictException($"User with ID {userId} is already a member of group {groupId}.");

/// <summary>Частки витрати некоректні (суми не сходяться, дублікати тощо) → 400.</summary>
public class InvalidExpenseSplitException(string message) : DomainException(message);

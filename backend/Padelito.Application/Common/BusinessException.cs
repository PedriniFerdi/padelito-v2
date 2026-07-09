namespace Padelito.Application.Common;

public sealed class BusinessException(string message) : Exception(message);

namespace PsiAgenda.Domain.Common;

/// <summary>Violacao de regra de negocio. A API traduz em HTTP 400/409.</summary>
public class DomainException(string message) : Exception(message);

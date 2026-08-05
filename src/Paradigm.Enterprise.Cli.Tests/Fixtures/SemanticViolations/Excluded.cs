namespace SemanticViolations;

public sealed class ExcludedOrderService
{
    public void Bypass(Order order) => order.Status = "excluded";
}

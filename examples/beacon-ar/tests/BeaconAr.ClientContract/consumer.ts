import type { IClient } from './generated/beacon-ar-v1';

export function exerciseAllApiGroups(client: IClient): readonly unknown[] {
  return [
    client.getCurrentUser(),
    client.getDashboardSummary(),
    client.searchProducts(undefined, 1, 10, undefined, undefined, true),
    client.searchCustomers(undefined, 1, 10, undefined, undefined, true),
    client.searchAddresses(undefined, 1, 10, undefined, undefined, undefined, undefined, undefined),
    client.searchCarriers(undefined, 1, 10, undefined, undefined, true),
    client.searchQuotes(undefined, undefined, undefined, 1, 10, undefined, undefined),
    client.searchSalesOrders(undefined, undefined, undefined, undefined, 1, 10, undefined, undefined)
  ];
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface TableView { id: string; seats: number; activeOrderId: string | null; status: string | null; }
export interface MenuItem { code: string; name: string; price: number; }
export interface InventoryItem { sku: string; name: string; quantityOnHand: number; quantityReserved: number; }
export interface OrderLine { menuCode: string; name: string; quantity: number; unitPrice: number; lineTotal: number; }
export interface OrderView { id: string; tableId: string; guests: number; status: string; lines: OrderLine[]; total: number; openedAtUtc: string; }
export interface OperationsView {
  profile: { name: string; minSeats: number; maxSeats: number };
  tables: TableView[];
  menu: MenuItem[];
  inventory: InventoryItem[];
  openOrders: OrderView[];
  summary: { configuredSeats: number; servedGuests: number; ordersClosed: number; revenue: number };
}

@Injectable({ providedIn: 'root' })
export class OperationsApi {
  private readonly http = inject(HttpClient);
  getOperations() { return this.http.get<OperationsView>('/api/operations'); }
  createOrder(tableId: string, guests: number) { return this.http.post('/api/orders', { tableId, guests }); }
  changeItem(orderId: string, menuCode: string, quantity: number, remove: boolean) {
    return this.http.post(`/api/orders/${orderId}/items${remove ? '/remove' : ''}`, { menuCode, quantity });
  }
  advance(orderId: string, action: string, method: string) {
    return this.http.post(`/api/orders/${orderId}/${action}`, action === 'checkout' ? { method } : {});
  }
}

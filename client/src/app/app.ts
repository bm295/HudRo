import { Component, OnInit, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, Observable } from 'rxjs';
import { OperationsApi, OperationsView, OrderView } from './operations-api';

@Component({
  selector: 'app-root',
  imports: [CurrencyPipe, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly api = inject(OperationsApi);
  operations: OperationsView | null = null;
  tableId = '';
  guests = 2;
  busy = false;
  message = '';
  error = false;
  readonly statusText: Record<string, string> = {
    Draft: 'Đang gọi món', SentToKitchen: 'Đã gửi bếp', Preparing: 'Bếp đang làm',
    Served: 'Đã phục vụ', Paid: 'Đã thanh toán', Closed: 'Đã đóng'
  };

  ngOnInit(): void { void this.load(); }

  get availableTables() { return this.operations?.tables.filter(table => !table.activeOrderId) ?? []; }

  async load(): Promise<void> {
    try {
      this.operations = await firstValueFrom(this.api.getOperations());
      if (!this.availableTables.some(table => table.id === this.tableId)) {
        this.tableId = this.availableTables[0]?.id ?? '';
      }
    } catch (error) { this.showError(error); }
  }

  async createOrder(): Promise<void> {
    if (!this.tableId || !Number.isInteger(this.guests) || this.guests < 1) return;
    await this.run(() => this.api.createOrder(this.tableId, this.guests));
  }

  async changeItem(order: OrderView, menuCode: string, quantity: number, remove = false): Promise<void> {
    if (!menuCode || !Number.isInteger(quantity) || quantity < 1) return;
    await this.run(() => this.api.changeItem(order.id, menuCode, quantity, remove));
  }

  async advance(order: OrderView, action: 'send' | 'prepare' | 'serve' | 'checkout', method = 'Card'): Promise<void> {
    await this.run(() => this.api.advance(order.id, action, method));
  }

  private async run(operation: () => Observable<unknown>): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    try {
      await firstValueFrom(operation());
      this.operations = await firstValueFrom(this.api.getOperations());
      if (!this.availableTables.some(table => table.id === this.tableId)) {
        this.tableId = this.availableTables[0]?.id ?? '';
      }
      this.message = 'Đã cập nhật.';
      this.error = false;
    } catch (error) { this.showError(error); }
    finally { this.busy = false; }
  }

  private showError(error: unknown): void {
    const response = error as { error?: { error?: string }; message?: string };
    this.message = response.error?.error ?? response.message ?? 'Thao tác thất bại.';
    this.error = true;
  }
}

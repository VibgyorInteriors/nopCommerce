# Quotation API Plugin for Angular

This plugin provides RESTful Web API endpoints for your Angular quotation application.

## Features

- **Products API**: Get all products or a specific product by ID
- **Discounts API**: Get all available discounts
- **Quotations API**: Create and manage quotations (placeholder - implement as needed)

## API Endpoints

### Products
- `GET /api/quotation/products` - Get all products (with pagination)
- `GET /api/quotation/products/{id}` - Get product by ID

### Discounts
- `GET /api/quotation/discounts` - Get all discounts

### Quotations
- `GET /api/quotation/quotations` - Get all quotations
- `POST /api/quotation/quotations` - Create a new quotation

## Installation

1. Build the plugin: The plugin will be automatically built when you build the main project
2. Go to Admin Panel → Configuration → Plugins → Local plugins
3. Find "Quotation API for Angular" and click "Install"
4. Click "Configure" to set up the plugin

## Usage with Angular

### Example Service

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class QuotationApiService {
  private apiUrl = 'https://your-nopcommerce-site.com/api/quotation';

  constructor(private http: HttpClient) {}

  getProducts(pageIndex = 0, pageSize = 100): Observable<any> {
    return this.http.get(`${this.apiUrl}/products?pageIndex=${pageIndex}&pageSize=${pageSize}`);
  }

  getProduct(id: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/products/${id}`);
  }

  getDiscounts(): Observable<any> {
    return this.http.get(`${this.apiUrl}/discounts`);
  }

  createQuotation(quotation: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/quotations`, quotation);
  }
}
```

### Authentication

The API uses nopCommerce's admin authentication. You'll need to:
1. Authenticate users through nopCommerce's login
2. Include authentication tokens in your Angular HTTP requests
3. Or implement API key authentication (extend the plugin)

## Extending the Plugin

To add more functionality:

1. **Add Quotation Storage**: Create a service to store quotations in the database
2. **Add More Endpoints**: Extend `QuotationApiController` with new actions
3. **Add Validation**: Add request validation and error handling
4. **Add API Keys**: Implement API key authentication for external access

## Notes

- All endpoints require admin authentication
- The quotation endpoints are placeholders - implement the business logic as needed
- This plugin doesn't modify core nopCommerce files - safe for updates


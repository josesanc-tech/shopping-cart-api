# Shopping Cart API — .NET 10

API REST de carrito de compras con autenticación JWT.

**Stack:** ASP.NET Core 10 · Entity Framework Core 10 · SQL Server 2022 · JWT · Docker

---

## Requisitos previos
# 1. Clonar el Repositorio
git clone https://github.com/josesanc-tech/shopping-cart-api.git
cd shopping-cart-api

git checkout develop

### Opción Docker (recomendada)

- **Docker Desktop**.
- Puertos libres: `5000` (API) y `1433` (SQL Server).

### Opción sin Docker

- **.NET 10 SDK** — descarcar .NET 10
- **SQL Server 2022** tener sql en su ordenador

---

## Instalación y ejecución con Docker

Desde la raíz de `shopping-cart-api`:

```bash
docker-compose up --build
```

Este comando:
1. Levanta **SQL Server 2022** en un contenedor.
2. Espera a que el healthcheck de SQL Server pase.
3. Compila y publica la **API en .NET 10**.
4. Al arrancar, aplica migraciones pendientes y ejecuta el seeder de datos.

**Verificar que está corriendo:**

- API: http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger

**Detener:**

```bash
docker-compose down            # detiene y elimina contenedores
docker-compose down -v         # además elimina el volumen de datos
```

---

## Instalación y ejecución sin Docker

### Levantar SQL Server

```bash
docker run -d --name sqlserver \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=Milu1234" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

O usar una instancia local existente y ajustar la cadena de conexión en el siguiente paso.

### 2. Configurar `appsettings.Development.json`

Crear el archivo `src/ShoppingCart.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ShoppingCartDb;User Id=sa;Password=Milu1234;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "CLAVE_SECRETA1234567891234567891",
    "Issuer": "ShoppingCartApi",
    "Audience": "ShoppingCartApp",
    "ExpiresMinutes": 60
  }
}
```

> Este archivo está en `.gitignore`. La clave JWT debe tener al menos **32 caracteres**.

### 3. Aplicar migraciones (opcional)

La API aplica migraciones automáticamente al arrancar. Para hacerlo manualmente:

```bash
dotnet ef database update \
  --project src/ShoppingCart.Infrastructure \
  --startup-project src/ShoppingCart.Api
```

### 4. Ejecutar la API

```bash
dotnet run --project src/ShoppingCart.Api
```

API en http://localhost:5000 · Swagger en http://localhost:5000/swagger

---

## Usuarios y datos semilla

El seeder corre **solo si la tabla `Users` está vacía**. Ejecutar la API múltiples veces no duplica datos.

### Usuarios de prueba

| Usuario | Contraseña | Rol |
|---------|-----------|-----|
| `cliente` | `Cliente123!` | Customer |
| `admin` | `Admin123!` | Admin |

### Productos de prueba

| Código | Nombre | Categoría | Precio | Stock |
|--------|--------|-----------|--------|-------|
| P001 | Laptop Pro 15" | Electrónica | $1200.00 | 10 |
| P002 | Mouse Inalámbrico | Accesorios | $25.00 | 50 |
| P003 | Teclado Mecánico RGB | Accesorios | $80.00 | 30 |
| P004 | Monitor 27" 4K | Electrónica | $350.00 | 15 |
| P005 | Webcam HD 1080p | Accesorios | $45.00 | **0** |
| P006 | Auriculares Bluetooth | Audio | $120.00 | 20 |

> **P005 tiene stock 0 a propósito** para probar el flujo de "producto agotado" y el error 409.

---

## Pruebas

### Tests unitarios

```bash
dotnet test
```

**Resultado esperado:**

```
Passed!  - Failed: 0, Passed: 17, Skipped: 0, Total: 17
```

**Cobertura:**

- **CartService** (13 tests): validaciones de cantidad, stock, existencia, acumulación, merge de items, descuento 10%, caso borde de $100 exactos, remove y clear.
- **OrderService** (4 tests): carrito vacío, creación de orden con descuento, decremento de stock, vaciado de carrito, **rollback completo ante stock insuficiente**, filtrado de historial por usuario.

### Prueba manual end-to-end (Swagger)

1. Abrir http://localhost:5000/swagger
2. Ejecutar `POST /api/auth/login` con:
   ```json
   { "username": "cliente", "password": "Cliente123!" }
   ```
3. Copiar el `token` de la respuesta.
4. Clic en el botón **Authorize**  pegar solo el token (sin "Bearer")  Authorize.
5. Ejecutar `GET /api/products`  deben aparecer 6 productos, `P005` con `stock: 0`.
6. Ejecutar `GET /api/products?search=laptop`  1 resultado.
7. Ejecutar `GET /api/products?category=Accesorios`  3 resultados.
8. Ejecutar `POST /api/cart/items` con:
   ```json
   { "productId": 1, "quantity": 2 }
   ```
   → Verificar en la respuesta `subtotal: 2400`, `discountAmount: 240`, `totalAmount: 2160` (descuento del 10%).
9. Ejecutar `POST /api/cart/items` con:
   ```json
   { "productId": 5, "quantity": 1 }
   ```
   → Debe devolver **409 Conflict** con detalle del producto agotado.
10. Ejecutar `POST /api/orders`  **201 Created** con `orderNumber`.
11. Ejecutar `GET /api/cart`  carrito vacío.
12. Ejecutar `GET /api/orders`  1 orden en el historial.
13. Ejecutar `GET /api/products/1`  `stock` bajó de 10 a **8**.

---

## Endpoints

| Método | Endpoint | Descripción | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/login` | Autenticar y obtener JWT | bloquea |
| GET | `/api/products` | Listar productos (`?search=`, `?category=`) | okey |
| GET | `/api/products/{id}` | Detalle de un producto | okey |
| GET | `/api/cart` | Carrito del usuario | okey |
| POST | `/api/cart/items` | Agregar producto al carrito | okey |
| PUT | `/api/cart/items/{productId}` | Actualizar cantidad | okey |
| DELETE | `/api/cart/items/{productId}` | Eliminar producto del carrito | okey |
| DELETE | `/api/cart` | Vaciar carrito | okey |
| POST | `/api/orders` | Finalizar compra | okey |
| GET | `/api/orders` | Historial de compras | okey |
| GET | `/api/orders/{id}` | Detalle de una orden | okey |

### Códigos HTTP

| Código | Cuándo |
|--------|--------|
| 200 | Consulta exitosa |
| 201 | Orden creada |
| 204 | Operación exitosa sin contenido |
| 400 | Request inválido (cantidad ≤ 0, carrito vacío) |
| 401 | Token ausente, expirado o inválido |
| 404 | Recurso no encontrado |
| 409 | Stock insuficiente o conflicto de concurrencia |
| 500 | Error interno no controlado |

Los errores de negocio devuelven **RFC 7807 Problem Details**:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "Stock insuficiente para 'Webcam HD 1080p'. Disponible: 0"
}
```

**Autor:** José Sánchez
**Fecha:** 20/09/2026

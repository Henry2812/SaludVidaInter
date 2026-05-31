# Migracion visual y funcional a Blazor WebAssembly

## Pantallas

| Pantalla | Razor | Layout | Funcion |
| --- | --- | --- | --- |
| Tienda cliente | `Pages/Storefront.razor` | `Layout/StoreLayout.razor` | Catalogo publico responsive, busqueda, categorias y carrito visual. |
| Login admin | `Pages/AdminLogin.razor` | `Layout/StoreLayout.razor` | Acceso oculto en `/admin/login` para entrar al panel. |
| Panel principal | `Pages/Dashboard.razor` | `Layout/AdminLayout.razor` | Metricas simuladas del negocio. |
| Productos | `Pages/Products.razor` | `Layout/AdminLayout.razor` | Tabla administrativa de inventario. |
| Personalizar PWA | `Pages/Customize.razor` | `Layout/AdminLayout.razor` | Edicion en memoria de marca, color y banner. |

## Componentes reutilizables

- `Shared/AdminSidebar.razor`: navegacion admin.
- `Shared/AdminTopBar.razor`: barra superior admin.
- `Shared/StoreHeader.razor`: encabezado de tienda y carrito.
- `Shared/CategoryFilter.razor`: filtros por categoria.
- `Shared/ProductCard.razor`: tarjeta de producto para cliente.
- `Shared/ProductTable.razor`: tabla de inventario admin.
- `Shared/StatCard.razor`: tarjeta de metrica.
- `Shared/BrandPreview.razor`: previsualizacion visual de marca.

## Modelos

- `Models/Product.cs`: producto del catalogo.
- `Models/ProductStatus.cs`: estado de inventario.
- `Models/AppearanceSettings.cs`: configuracion visual.
- `Models/DashboardStat.cs`: metrica del dashboard.

## Servicios

- `Services/IProductService.cs` y `Services/ProductService.cs`: productos, categorias y metricas simuladas.
- `Services/IAppearanceService.cs` y `Services/AppearanceService.cs`: configuracion de marca en memoria.
- `Services/CartService.cs`: carrito temporal en memoria.
- `Services/AuthService.cs`: login real contra Supabase Auth usando REST y sesion en `localStorage`.

## Rutas publicas y privadas

- `/` y `/home`: tienda publica para clientes.
- `/admin/login`: login oculto para administrador.
- `/admin`, `/admin/productos`, `/admin/personalizar`: rutas protegidas por `Layout/AdminLayout.razor`.

Configuracion de Supabase:

- Archivo: `wwwroot/appsettings.json`
- Necesitas pegar `Supabase:Url` y `Supabase:AnonKey`.
- Crea el usuario admin en Supabase Dashboard > Authentication > Users.

Supabase se usara para:

- Auth: login del administrador.
- Database: tablas relacionales en PostgreSQL para productos, categorias y configuracion.
- Storage: bucket para imagenes de productos.

## Setup de productos

Ejecuta `SUPABASE_SETUP.sql` en Supabase > SQL Editor antes de usar el CRUD de productos.
El archivo crea:

- Tabla `public.products`.
- Bucket publico `product-images`.
- Politicas RLS para lectura publica y escritura con usuario autenticado.

## Deploy en Vercel

Se agrego `vercel.json` para publicar Blazor WebAssembly como sitio estatico.

Configuracion esperada:

- Build command: `npm run build` o el `buildCommand` de `vercel.json`.
- Output directory: `deploy`.
- Rewrites: todo apunta a `/index.html` para que rutas como `/admin/productos` funcionen.

## CSS global

Debe permanecer en `wwwroot/css/app.css`:

- Variables de tema: colores, sombra, fondo, linea.
- Reset/base: `box-sizing`, `body`, `button`, `input`, `img`.
- Animacion comun `.view`.
- Pantalla de carga `.loading-shell`.
- Estado 404 `.empty-state`.

## CSS aislado

Debe ir en `.razor.css`:

- Layouts: grid admin, contenedor tienda.
- Componentes: sidebar, topbar, tarjetas, tablas, filtros.
- Paginas: hero de tienda, grid de productos, formularios de personalizacion, dashboard.

## Siguiente conexion a backend

Cuando agreguemos BD/API, el primer cambio debe ocurrir en `ProductService` y `AppearanceService`.
Las paginas y componentes ya consumen interfaces, asi que no deberian necesitar cambios grandes.

# La Casita de Miga - Backend API 🥐📊

API REST robusta y escalable desarrollada en .NET 8 (C#), completamente dockerizada y desplegada en la nube a través de Render, utilizando Neon Database (PostgreSQL Serverless) como motor de persistencia. El proyecto adopta un enfoque moderno Cloud-Native para el sector gastronómico/retail, resolviendo problemas reales de negocio, lógica financiera e inventario.

## 🌐 Despliegue en Producción (Live Demo)

El sistema se encuentra configurado con integración continua y desplegado en entornos productivos de acceso público:

* API Backend & Panel de Pruebas: 🛠️ [Explorar Endpoints (Swagger UI)](https://lacasitademiga.onrender.com/swagger/index.html)
* Hosting de Servicios: Contenedor administrado, construido mediante Dockerfile y desplegado en Render 🚀
* Base de Datos: Motor PostgreSQL alojado en la nube de forma serverless en Neon Database 🐘

## 💡 Problemas de Negocio que Resuelve

* **Pérdida de Margen por Inflación:** Implementa el cálculo de Costo Promedio Ponderado (CPP) en el ingreso de stock y congela los costos históricos en cada venta para reportar ganancias netas reales exactas en el Dashboard.
* **Fallas de Stock por Concurrencia:** Protege el inventario ante compras simultáneas en ráfaga mediante transacciones ACID y Concurrencia Optimista, evitando la sobreventa de productos.
* **Altos Costos de Infraestructura (APIs):** Resuelve el perímetro de entrega (radio de 15 km) de forma local usando la fórmula matemática de Haversine combinada con Google Geocoding, ahorrando miles de peticiones pagas a la API de rutas de Google.
* **Cobros Manuales y Conciliación de Pagos:** Integra Mercado Pago (Checkout Pro) para automatizar el cobro online y la confirmación de pagos vía webhooks, eliminando la verificación manual de transferencias.
* **Acceso No Autorizado a Datos:** Autenticación JWT real (no solo generación de tokens) con autorización por roles y verificación de propiedad de recursos, evitando que un cliente autenticado pueda ver o modificar datos de otro usuario.
* **Latencia y Costos de Cómputo en Consultas Frecuentes:** Cachea las consultas de productos más solicitadas con Redis (Upstash), reduciendo la carga sobre la base serverless y el tiempo de respuesta en catálogos de alto tráfico.

## 💳 Pasarela de Pagos (Mercado Pago)

Integración con **Mercado Pago Checkout Pro** para el cobro online de pedidos, con confirmación asincrónica vía webhooks.

### Flujo de pago

1. El cliente arma su pedido y se crea una `Order` en estado `Pending`.
2. El backend genera una **preferencia de pago** (`POST /api/payment/{orderId}/preference`), armando el detalle de ítems a partir de los precios ya congelados en la orden (nunca se confía en montos enviados por el cliente).
3. Se redirige al cliente al checkout de Mercado Pago con el link (`init_point`) devuelto.
4. Mercado Pago notifica el resultado del pago de forma asincrónica a un **webhook** (`POST /api/payment/webhook`), que:
   * Filtra los eventos, procesando únicamente los de tipo `payment` (ignora `merchant_order` y otros topics).
   * Nunca confía en el estado que llega en el body de la notificación: siempre **consulta el pago real** contra la API de Mercado Pago usando el ID recibido.
   * Vincula la notificación a la `Order` correspondiente mediante `external_reference`.
   * Aplica una verificación de **idempotencia**: si la orden ya no está en estado `Pending`, la notificación se ignora, evitando reprocesar pagos duplicados (Mercado Pago reintenta notificaciones por diseño).
   * Actualiza el estado de la orden (`Paid` / `Cancelled`) reutilizando la lógica de negocio existente de `IOrderService`, que además gestiona la devolución de stock ante cancelaciones.

### Seguridad

* El backend **nunca marca un pago como aprobado en base al payload del webhook**: todo estado se verifica contra la API oficial de Mercado Pago antes de aplicar cambios.
* Se implementó y verificó de forma independiente (C# y Python) el cálculo de validación de firma HMAC-SHA256 (`x-signature` / `x-request-id`) documentado por Mercado Pago. La verificación quedó **documentada pero deshabilitada temporalmente** en el entorno de pruebas: el hash calculado con las credenciales de sandbox confirmadas nunca coincidió con el valor recibido, una inconsistencia reproducible que apunta a un comportamiento propio del ambiente de pruebas de Mercado Pago. Queda pendiente de validar en modo productivo.

### Variables de entorno adicionales

```
MercadoPago:AccessToken     (Access Token de la cuenta de Mercado Pago)
MercadoPago:WebhookSecret   (Clave secreta para validar la firma del webhook)
MercadoPago:PublicBaseUrl   (URL pública del backend, para armar las notification/back URLs)
```

## 🔐 Autenticación y Autorización

Autenticación basada en **JWT**, validada en cada request (no solo generada en el login), con autorización por roles y protección contra acceso indebido a datos de otros usuarios.

* **Roles:** `Customer` y `Admin`. Los endpoints de catálogo (productos, marcas, categorías) son de lectura pública; la escritura (crear/editar/eliminar) está restringida a `Admin`. Endpoints sensibles como el dashboard de rentabilidad o la gestión de usuarios son exclusivos de `Admin`.
* **Protección IDOR (Insecure Direct Object Reference):** en endpoints de órdenes, se verifica que el `CustomerId` del recurso solicitado coincida con el usuario autenticado (o que sea `Admin`), evitando que un cliente pueda consultar o crear órdenes a nombre de otro usuario simplemente cambiando un ID en la URL.
* **Swagger** integrado con soporte de token Bearer para probar endpoints protegidos directamente desde la documentación interactiva.

## ⚡ Caché de Productos (Redis / Upstash)

Las consultas de lectura de productos (`GetById`, `GetBySlug`, listado paginado) se cachean con **Redis**, reduciendo la carga sobre la base de datos y el tiempo de respuesta en catálogos de alto tráfico.

* **Invalidación por clave directa** para consultas individuales (`GetById` / `GetBySlug`): al crear, editar o eliminar un producto o variante, se elimina puntualmente la clave afectada.
* **Invalidación por versión** para el listado paginado: dado que existen múltiples combinaciones de filtros (categoría, marca, página), se utiliza un contador de versión en Redis que se incrementa ante cualquier escritura, invalidando de forma atómica todas las combinaciones cacheadas sin necesidad de enumerarlas una por una.
* **Degradación elegante (fail-open):** si Redis no está disponible, el sistema no falla — cae automáticamente a consultar Postgres directamente, logueando la incidencia.

## 📋 Logging Estructurado (Serilog + Better Stack)

Logging estructurado con **Serilog**, con doble destino según severidad:

* **Consola:** recibe todo desde nivel `Information` (incluye el log automático de cada request HTTP), útil para desarrollo y debugging en vivo.
* **Better Stack** (servicio externo, persistente): recibe únicamente `Warning` en adelante — excepciones no controladas, errores de negocio relevantes, fallas de servicios externos (Mercado Pago, Google, Brevo) — manteniendo el volumen de logs persistidos enfocado en señal real, no en ruido operativo normal.
* El manejo global de excepciones (`GlobalExceptionHandler`) clasifica cada error por tipo y decide su severidad de log automáticamente: un 500 no controlado se registra con `LogError` (incluyendo stack trace), mientras que errores de negocio esperables (404, 400, 401, 409) se registran como `LogWarning`, evitando que eventos normales (como una contraseña incorrecta) se traten como fallas críticas del sistema.

## 🏗️ Arquitectura y Buenas Prácticas

* **Vertical Slice Architecture (Componentes Verticales):** El código se organiza por funcionalidades autónomas (`Users`, `Products`, `Orders`, `Payments`, etc.) en lugar de capas rígidas tradicionales. Esto maximiza la cohesión y facilita el mantenimiento.
* **Estructura de Datos Dinámica:** Manejo de variantes de productos (ej: sabores, tamaños) mediante diccionarios en C# serializados transparentemente en columnas JSONB de PostgreSQL.
* **Contenedores & DevOps (Dockerfile):** Incluye configuración de Docker para garantizar la portabilidad absoluta del sistema, facilitando que corra idénticamente en desarrollo local o en orquestadores de la nube.
* **Estrategia Cloud-Native (Render + Neon):** Configuración automatizada de puertos mediante variables de entorno e inicialización con Auto-Migraciones en el arranque del contenedor, garantizando despliegues continuos sin intervención manual.
* **Testing:** Cobertura de tests unitarios con xUnit sobre los servicios de negocio principales.

## 🛠️ Stack Tecnológico

* **Lenguaje & Framework:** .NET 8 / C#
* **Contenedores:** Docker (Dockerfile multi-stage optimizado)
* **Base de Datos:** PostgreSQL (Neon Serverless con soporte nativo JSONB)
* **ORM:** Entity Framework Core (Code-First)
* **Identidad & Seguridad:** JWT (JSON Web Tokens) con validación activa por middleware, autorización por roles, BCrypt, e integración con Google OAuth (IdToken)
* **Caché:** Redis (Upstash Serverless)
* **Pagos:** Mercado Pago SDK (Checkout Pro, Webhooks)
* **Logging:** Serilog con sink a Better Stack (logging estructurado y persistente)
* **Comunicaciones & Errores:** HttpClient Factory (Integración con API REST de Brevo para Emails) y control global de excepciones.
* **Testing:** xUnit

## 🚀 Configuración Rápida (Variables de Entorno)

Proveer las siguientes claves en el entorno de producción de Render:

* `ConnectionStrings:PostgresConnection` (String de conexión de Neon)
* `Jwt:Key`, `Jwt:Issuer` & `Jwt:Audience` (`Jwt:Key` de mínimo 256 bits)
* `GoogleMaps:ApiKey` & `Brevo:ApiKey`
* `MercadoPago:AccessToken`, `MercadoPago:WebhookSecret` & `MercadoPago:PublicBaseUrl`
* `Redis:ConnectionString` (conexión a Upstash)
* `BetterStack:SourceToken` & `BetterStack:IngestingHost`

## ✒️ Contacto & Autor

* Roberto Torre - Desarrollador Backend .NET
* LinkedIn: [linkedin.com/in/torre-roberto](https://www.linkedin.com/in/torre-roberto)
* Correo Electrónico: [torreroberto1996@gmail.com](mailto:torreroberto1996@gmail.com)
* Teléfono/WhatsApp: [+54 9 11 6249-1310](https://wa.me/5491162491310)
* GitHub: [github.com/RobertoTorre96](https://github.com/RobertoTorre96?tab=repositories) 📂 (¡Te invito a explorar mis otros repositorios!)

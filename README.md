# La Casita de Miga - Backend API 🥐📊

API REST robusta y escalable desarrollada en **.NET 9 (C#)**, completamente **dockerizada** y desplegada en la nube a través de **Render**, utilizando **Neon Database (PostgreSQL Serverless)** como motor de persistencia. El proyecto adopta un enfoque moderno *Cloud-Native* para el sector gastronómico/retail, resolviendo problemas reales de negocio, lógica financiera e inventario.

---

## 🌐 Despliegue en Producción (Live Demo)

El sistema se encuentra configurado con integración continua y desplegado en entornos productivos de acceso público:

* **API Backend & Panel de Pruebas:** 🛠️ [Explorar Endpoints (Swagger UI)](https://lacasitademiga.onrender.com/swagger/index.html)
* **Hosting de Servicios:** Contenedor administrado, construido mediante **Dockerfile** y desplegado en **Render** 🚀
* **Base de Datos:** Motor PostgreSQL alojado en la nube de forma serverless en **Neon Database** 🐘

---

## 💡 Problemas de Negocio que Resuelve

* **Pérdida de Margen por Inflación:** Implementa el cálculo de **Costo Promedio Ponderado (CPP)** en el ingreso de stock y congela los costos históricos en cada venta para reportar ganancias netas reales exactas en el Dashboard.
* **Fallas de Stock por Concurrencia:** Protege el inventario ante compras simultáneas en ráfaga mediante transacciones ACID y **Concurrencia Optimista**, evitando la sobreventa de productos.
* **Altos Costos de Infraestructura (APIs):** Resuelve el perímetro de entrega (radio de 15 km) de forma local usando la fórmula matemática de **Haversine** combinada con Google Geocoding, ahorrando miles de peticiones pagas a la API de rutas de Google.

---

## 🏗️ Arquitectura y Buenas Prácticas

* **Vertical Slice Architecture (Componentes Verticales):** El código se organiza por funcionalidades autónomas (`Users`, `Products`, `Orders`, etc.) en lugar de capas rígidas tradicionales. Esto maximiza la cohesión y facilita el mantenimiento.
* **Estructura de Datos Dinámica:** Manejo de variantes de productos (ej: sabores, tamaños) mediante diccionarios en C# serializados transparentemente en columnas **JSONB** de PostgreSQL.
* **Contenedores & DevOps (Dockerfile):** Incluye configuración de **Docker** para garantizar la portabilidad absoluta del sistema, facilitando que corra idénticamente en desarrollo local o en orquestadores de la nube.
* **Estrategia Cloud-Native (Render + Neon):** Configuración automatizada de puertos mediante variables de entorno e inicialización con **Auto-Migraciones** en el arranque del contenedor, garantizando despliegues continuos sin intervención manual.

---

## 🛠️ Stack Tecnológico

* **Lenguaje & Framework:** .NET 8 / C#
* **Contenedores:** Docker (Dockerfile multi-stage optimizado)
* **Base de Datos:** PostgreSQL (Neon Serverless con soporte nativo JSONB)
* **ORM:** Entity Framework Core (Code-First)
* **Identidad & Seguridad:** JWT (JSON Web Tokens), BCrypt, e integración con Google OAuth (IdToken)
* **Comunicaciones & Errores:** HttpClient Factory (Integración con API REST de Brevo para Emails) y control global de excepciones bajo el estándar industrial **RFC 7807 (Problem Details)**.

---

## 🚀 Configuración Rápida (Variables de Entorno)

Proveer las siguientes claves en el entorno de producción de Render:
* `ConnectionStrings:PostgresConnection` (String de conexión de Neon)
* `Jwt:Key` (Mínimo 256 bits)
* `GoogleMaps:ApiKey` & `Brevo:ApiKey`

---

## ✒️ Contacto & Autor

* **Roberto Torre** - *Desarrollador Backend .NET*
* **LinkedIn:** [linkedin.com/in/torre-roberto](https://www.linkedin.com/in/torre-roberto)
* **Correo Electrónico:** torreroberto1996@gmail.com
* **Teléfono/WhatsApp:** [+54 9 11 6249-1310](https://wa.me/5491162491310)

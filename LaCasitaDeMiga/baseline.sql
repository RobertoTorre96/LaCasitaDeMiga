CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE brands (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    logo_url character varying(255) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_brands" PRIMARY KEY (id)
);

CREATE TABLE categories (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    slug character varying(120) NOT NULL,
    parent_id uuid,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_categories" PRIMARY KEY (id),
    CONSTRAINT "FK_categories_categories_parent_id" FOREIGN KEY (parent_id) REFERENCES categories (id) ON DELETE SET NULL
);

CREATE TABLE "Orders" (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id")
);

CREATE TABLE products (
    id uuid NOT NULL,
    name character varying(150) NOT NULL,
    slug character varying(180) NOT NULL,
    description text NOT NULL,
    category_id uuid NOT NULL,
    brand_id uuid,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_products" PRIMARY KEY (id),
    CONSTRAINT "FK_products_brands_brand_id" FOREIGN KEY (brand_id) REFERENCES brands (id) ON DELETE SET NULL,
    CONSTRAINT "FK_products_categories_category_id" FOREIGN KEY (category_id) REFERENCES categories (id) ON DELETE RESTRICT
);

CREATE TABLE product_variants (
    id uuid NOT NULL,
    product_id uuid NOT NULL,
    sku character varying(50) NOT NULL,
    price numeric(12,2) NOT NULL,
    compare_at_price numeric(12,2),
    stock integer NOT NULL DEFAULT 0,
    low_stock_threshold integer NOT NULL DEFAULT 3,
    attributes jsonb NOT NULL DEFAULT ('{}'::jsonb),
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_product_variants" PRIMARY KEY (id),
    CONSTRAINT "FK_product_variants_products_product_id" FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE
);

CREATE TABLE "OrderItems" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "ProductVariantId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrderItems_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES product_variants (id) ON DELETE RESTRICT
);

CREATE INDEX "IX_categories_parent_id" ON categories (parent_id);

CREATE UNIQUE INDEX "IX_categories_slug" ON categories (slug);

CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");

CREATE INDEX "IX_OrderItems_ProductVariantId" ON "OrderItems" ("ProductVariantId");

CREATE INDEX idx_variants_product ON product_variants (product_id);

CREATE UNIQUE INDEX product_variants_sku_key ON product_variants (sku);

CREATE INDEX idx_products_category ON products (category_id);

CREATE UNIQUE INDEX idx_products_slug ON products (slug);

CREATE INDEX "IX_products_brand_id" ON products (brand_id);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260619185732_IgnorarPasado', '8.0.28');

COMMIT;


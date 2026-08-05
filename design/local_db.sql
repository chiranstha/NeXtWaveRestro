-- =============================================================================
-- RESTAURANT MOBILE APP  -  ON-DEVICE SQLite schema (Flutter / Drift / sqflite)
-- Purpose : offline-first local store. Menu = read cache; Orders/Tickets/Reviews
--           = read+write, queued for upload via the outbox.
-- Keys    : every writable row has a client_guid (TEXT, app-generated UUID v4)
--           AND a nullable server_id (INTEGER, filled after sync).
-- Money   : stored as INTEGER PAISA (amount * 100) to avoid float drift.
--           e.g. Rs 245.50 -> 24550. Convert at the UI edge only.
-- Dates   : ISO-8601 UTC strings in *_ad; Nepali 'yyyy-MM-dd' in *_bs.
-- Sync    : sync_state 0=synced 1=dirty(pending) 2=conflict; is_deleted soft flag.
-- =============================================================================

PRAGMA foreign_keys = ON;

-- -----------------------------------------------------------------------------
-- 0. SYNC INFRASTRUCTURE
-- -----------------------------------------------------------------------------

-- one row per syncable entity type: where the last pull left off
CREATE TABLE sync_cursor (
    entity_type   TEXT PRIMARY KEY,          -- 'MenuItem','Order','Ticket','Review'...
    last_seq      INTEGER NOT NULL DEFAULT 0,-- server ChangeLog.Seq high-water mark
    last_pulled_at TEXT
);

-- the outbox: every local create/update/delete the device still owes the server.
-- Drained FIFO by the sync worker; idempotent on the server via client_guid.
CREATE TABLE outbox (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type   TEXT    NOT NULL,
    entity_guid   TEXT    NOT NULL,          -- client_guid of the affected row
    operation     INTEGER NOT NULL,          -- 1=Insert,2=Update,3=Delete
    payload_json  TEXT    NOT NULL,          -- serialized DTO snapshot
    base_version  TEXT,                      -- server row_version we edited (conflict check)
    attempt_count INTEGER NOT NULL DEFAULT 0,
    last_error    TEXT,
    status        INTEGER NOT NULL DEFAULT 0,-- 0=Queued,1=Sending,2=Failed,3=Done
    created_at    TEXT    NOT NULL,
    batch_guid    TEXT                        -- groups one upload attempt
);
CREATE INDEX ix_outbox_status ON outbox(status, id);

-- local device identity / session
CREATE TABLE app_device (
    id            INTEGER PRIMARY KEY CHECK (id = 1), -- single row
    install_id    TEXT NOT NULL,
    server_device_id INTEGER,
    outlet_id     INTEGER,
    user_id       INTEGER,
    fcm_token     TEXT,
    last_full_sync_at TEXT
);

-- -----------------------------------------------------------------------------
-- 1. MENU  (read cache - pulled from server, never edited locally)
-- -----------------------------------------------------------------------------
CREATE TABLE menu_category (
    server_id     INTEGER PRIMARY KEY,
    parent_id     INTEGER,
    name          TEXT NOT NULL,
    name_np       TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    color_hex     TEXT,
    image_url     TEXT,
    is_active     INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE menu_item (
    server_id     INTEGER PRIMARY KEY,
    category_id   INTEGER NOT NULL,
    name          TEXT NOT NULL,
    name_np       TEXT,
    description   TEXT,
    base_price    INTEGER NOT NULL DEFAULT 0,   -- paisa
    station_type  INTEGER NOT NULL DEFAULT 1,   -- 1=Kitchen,2=Bar,3=Both -> KOT/BOT routing
    default_station_id INTEGER,
    prep_minutes  INTEGER,
    is_vatable    INTEGER NOT NULL DEFAULT 1,
    has_variants  INTEGER NOT NULL DEFAULT 0,
    has_modifiers INTEGER NOT NULL DEFAULT 0,
    is_available  INTEGER NOT NULL DEFAULT 1,   -- 86 toggle (server-pushed)
    is_active     INTEGER NOT NULL DEFAULT 1,
    thumbnail_url TEXT,
    is_veg        INTEGER,
    spice_level   INTEGER,
    avg_rating    REAL,
    rating_count  INTEGER NOT NULL DEFAULT 0,
    display_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (category_id) REFERENCES menu_category(server_id)
);
CREATE INDEX ix_item_cat ON menu_item(category_id, is_active, is_available);

CREATE TABLE menu_item_variant (
    server_id     INTEGER PRIMARY KEY,
    menu_item_id  INTEGER NOT NULL,
    name          TEXT NOT NULL,
    price_delta   INTEGER NOT NULL DEFAULT 0,   -- paisa
    is_absolute   INTEGER NOT NULL DEFAULT 0,
    is_default    INTEGER NOT NULL DEFAULT 0,
    display_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (menu_item_id) REFERENCES menu_item(server_id)
);

CREATE TABLE modifier_group (
    server_id     INTEGER PRIMARY KEY,
    name          TEXT NOT NULL,
    min_select    INTEGER NOT NULL DEFAULT 0,
    max_select    INTEGER NOT NULL DEFAULT 1,
    is_required   INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE modifier (
    server_id     INTEGER PRIMARY KEY,
    group_id      INTEGER NOT NULL,
    name          TEXT NOT NULL,
    price_delta   INTEGER NOT NULL DEFAULT 0,   -- paisa
    display_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (group_id) REFERENCES modifier_group(server_id)
);

CREATE TABLE item_modifier_group (
    menu_item_id  INTEGER NOT NULL,
    group_id      INTEGER NOT NULL,
    display_order INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (menu_item_id, group_id)
);

CREATE TABLE menu_item_image (
    server_id     INTEGER PRIMARY KEY,
    menu_item_id  INTEGER NOT NULL,
    url           TEXT NOT NULL,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_primary    INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (menu_item_id) REFERENCES menu_item(server_id)
);

-- -----------------------------------------------------------------------------
-- 2. TABLES (read cache + status writes)
-- -----------------------------------------------------------------------------
CREATE TABLE dining_table (
    server_id     INTEGER PRIMARY KEY,
    floor_name    TEXT,
    name          TEXT NOT NULL,
    seats         INTEGER NOT NULL DEFAULT 4,
    qr_code       TEXT,
    status        INTEGER NOT NULL DEFAULT 1,   -- 1=Free,2=Occupied,3=Reserved,4=Billed,5=Cleaning
    sync_state    INTEGER NOT NULL DEFAULT 0
);

-- -----------------------------------------------------------------------------
-- 3. ORDERS  (read+write, offline-capable)
-- -----------------------------------------------------------------------------
CREATE TABLE app_order (
    client_guid   TEXT PRIMARY KEY,             -- generated on device
    server_id     INTEGER,                      -- filled after first sync
    order_number  TEXT,
    order_type    INTEGER NOT NULL DEFAULT 1,   -- 1=DineIn,2=Takeaway,3=Delivery,4=QrSelfOrder
    table_id      INTEGER,
    table_session_guid TEXT,
    waiter_user_id INTEGER,
    customer_name TEXT,
    customer_phone TEXT,
    guest_count   INTEGER,
    status        INTEGER NOT NULL DEFAULT 1,   -- 1=Draft,2=Placed,3=InProgress,4=Served,5=Billed,6=Closed,7=Cancelled
    sub_total     INTEGER NOT NULL DEFAULT 0,   -- paisa
    discount_amount INTEGER NOT NULL DEFAULT 0,
    service_charge_amount INTEGER NOT NULL DEFAULT 0,
    vat_amount    INTEGER NOT NULL DEFAULT 0,
    grand_total   INTEGER NOT NULL DEFAULT 0,
    notes         TEXT,
    order_date_ad TEXT NOT NULL,
    order_date_bs TEXT,
    server_version TEXT,                         -- last known server row_version
    sync_state    INTEGER NOT NULL DEFAULT 1,   -- new local order starts dirty
    is_deleted    INTEGER NOT NULL DEFAULT 0,
    updated_at    TEXT NOT NULL
);
CREATE INDEX ix_order_status ON app_order(status, order_date_ad);
CREATE INDEX ix_order_sync ON app_order(sync_state);

CREATE TABLE app_order_item (
    client_guid   TEXT PRIMARY KEY,
    server_id     INTEGER,
    order_guid    TEXT NOT NULL,
    menu_item_id  INTEGER NOT NULL,
    variant_id    INTEGER,
    item_name     TEXT NOT NULL,                -- snapshot
    station_type  INTEGER NOT NULL,             -- snapshot -> KOT/BOT routing
    quantity      REAL NOT NULL DEFAULT 1,
    unit_price    INTEGER NOT NULL,             -- paisa
    modifier_total INTEGER NOT NULL DEFAULT 0,
    discount_amount INTEGER NOT NULL DEFAULT 0,
    line_total    INTEGER NOT NULL DEFAULT 0,
    course_no     INTEGER,
    special_instructions TEXT,
    status        INTEGER NOT NULL DEFAULT 1,   -- 1=New,2=Fired,3=Preparing,4=Ready,5=Served,6=Void
    void_reason   TEXT,
    fired_at_ad   TEXT,
    sync_state    INTEGER NOT NULL DEFAULT 1,
    is_deleted    INTEGER NOT NULL DEFAULT 0,
    updated_at    TEXT NOT NULL,
    FOREIGN KEY (order_guid) REFERENCES app_order(client_guid)
);
CREATE INDEX ix_oitem_order ON app_order_item(order_guid, status);

CREATE TABLE app_order_item_modifier (
    client_guid   TEXT PRIMARY KEY,
    server_id     INTEGER,
    order_item_guid TEXT NOT NULL,
    modifier_id   INTEGER NOT NULL,
    modifier_name TEXT NOT NULL,                -- snapshot
    price_delta   INTEGER NOT NULL DEFAULT 0,
    quantity      REAL NOT NULL DEFAULT 1,
    FOREIGN KEY (order_item_guid) REFERENCES app_order_item(client_guid)
);

-- -----------------------------------------------------------------------------
-- 4. TICKETS  (KOT / BOT) - created locally on "fire", bumped on KDS
-- -----------------------------------------------------------------------------
CREATE TABLE app_ticket (
    client_guid   TEXT PRIMARY KEY,
    server_id     INTEGER,
    order_guid    TEXT NOT NULL,
    station_id    INTEGER NOT NULL,
    ticket_number TEXT,
    ticket_kind   INTEGER NOT NULL,             -- 1=KOT,2=BOT
    ticket_type   INTEGER NOT NULL DEFAULT 1,   -- 1=New,2=Addon,3=Void,4=Reprint
    table_name    TEXT,
    course_no     INTEGER,
    status        INTEGER NOT NULL DEFAULT 1,   -- 1=Queued,2=Ack,3=Preparing,4=Ready,5=Bumped,6=Recalled,7=Cancelled
    priority      INTEGER NOT NULL DEFAULT 0,
    fired_at_ad   TEXT NOT NULL,
    ready_at_ad   TEXT,
    bumped_at_ad  TEXT,
    server_version TEXT,
    sync_state    INTEGER NOT NULL DEFAULT 1,
    updated_at    TEXT NOT NULL,
    FOREIGN KEY (order_guid) REFERENCES app_order(client_guid)
);
CREATE INDEX ix_ticket_board ON app_ticket(station_id, status, fired_at_ad);

CREATE TABLE app_ticket_item (
    client_guid   TEXT PRIMARY KEY,
    server_id     INTEGER,
    ticket_guid   TEXT NOT NULL,
    order_item_guid TEXT NOT NULL,
    name          TEXT NOT NULL,
    quantity      REAL NOT NULL,
    modifiers_text TEXT,                         -- flattened for the KDS card
    special_instructions TEXT,
    status        INTEGER NOT NULL DEFAULT 1,   -- 1=Queued,2=Preparing,3=Ready,4=Bumped,5=Void
    FOREIGN KEY (ticket_guid) REFERENCES app_ticket(client_guid)
);

-- -----------------------------------------------------------------------------
-- 5. REVIEWS  (write, queued for upload)
-- -----------------------------------------------------------------------------
CREATE TABLE app_review (
    client_guid   TEXT PRIMARY KEY,
    server_id     INTEGER,
    order_guid    TEXT,
    table_id      INTEGER,
    menu_item_id  INTEGER,                       -- NULL = whole-visit review
    customer_name TEXT,
    customer_phone TEXT,
    overall_rating INTEGER NOT NULL,
    food_rating   INTEGER,
    service_rating INTEGER,
    ambience_rating INTEGER,
    value_rating  INTEGER,
    comment       TEXT,
    source        INTEGER NOT NULL DEFAULT 1,
    review_date_ad TEXT NOT NULL,
    review_date_bs TEXT,
    sync_state    INTEGER NOT NULL DEFAULT 1,
    is_deleted    INTEGER NOT NULL DEFAULT 0,
    updated_at    TEXT NOT NULL
);
CREATE INDEX ix_review_sync ON app_review(sync_state);

CREATE TABLE app_review_media (
    client_guid   TEXT PRIMARY KEY,
    server_id     INTEGER,
    review_guid   TEXT NOT NULL,
    local_path    TEXT,                          -- on-device file awaiting upload
    media_url     TEXT,                          -- filled after blob upload
    media_type    INTEGER NOT NULL DEFAULT 1,
    upload_state  INTEGER NOT NULL DEFAULT 0,    -- 0=Pending,1=Uploaded
    FOREIGN KEY (review_guid) REFERENCES app_review(client_guid)
);

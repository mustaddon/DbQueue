CREATE TABLE "DbQueue"
(
    "Id" BIGSERIAL PRIMARY KEY,
    "Queue" VARCHAR(255) NOT NULL,
    "Data" BYTEA NOT NULL,
    "Hash" BIGINT NOT NULL,
    "IsBlob" BOOLEAN NOT NULL,
    "Type" VARCHAR(255) NULL,
    "AvailableAfter" TIMESTAMP NULL,
    "RemoveAfter" TIMESTAMP NULL,
    "LockId" BIGINT NULL
);

CREATE INDEX ON "DbQueue" ("Queue"); 
CREATE INDEX ON "DbQueue" ("Hash");
CREATE INDEX ON "DbQueue" ("LockId");
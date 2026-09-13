-- SQLite
CREATE TABLE IF NOT EXISTS JobDefinition (
    Id              INTEGER  NOT NULL,
    Name            TEXT     NOT NULL,
    Description     TEXT,
    ProfileName     TEXT     NOT NULL,
    RegionName      TEXT     NOT NULL,
    ServiceType     TEXT     NOT NULL,
    Operation       TEXT     NOT NULL,
    ParametersJson  TEXT     NOT NULL,
    CronExpression  TEXT     NOT NULL,
    CronTimeZone    TEXT     NOT NULL,
    IsEnabled       INTEGER  NOT NULL,
    CreatedAt       TEXT     NOT NULL,
    UpdatedAt       TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS JobExecutionLog (
    Id          INTEGER  NOT NULL,
    JobId       INTEGER  NOT NULL,
    JobName     TEXT     NOT NULL,
    StartedAt   TEXT     NOT NULL,
    FinishedAt  TEXT,
    Status      TEXT     NOT NULL,
    Message     TEXT,
    ErrorDetail TEXT,
    PRIMARY KEY (Id AUTOINCREMENT)
);

CREATE INDEX IF NOT EXISTS IX_JobExecutionLog_JobId_StartedAt ON JobExecutionLog (JobId, StartedAt DESC);

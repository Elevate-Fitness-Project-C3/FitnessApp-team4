DECLARE @MessageId bigint = NULL;
DECLARE @MaxAttempts int = 4;

IF @MessageId IS NULL OR @MessageId <= 0
    THROW 50001, 'Set MessageId to one reviewed failed outbox message.', 1;

UPDATE dbo.EmailOutbox
SET Attempts = 0, NextAttemptOn = SYSUTCDATETIME(), LockedUntil = NULL, LockToken = NULL
OUTPUT INSERTED.Id AS MessageId, DELETED.Attempts AS PreviousAttempts,
       SYSUTCDATETIME() AS RetriedOn, ORIGINAL_LOGIN() AS OperatorLogin
WHERE Id = @MessageId AND ProcessedOn IS NULL AND Attempts >= @MaxAttempts
  AND (LockedUntil IS NULL OR LockedUntil <= SYSUTCDATETIME());

IF @@ROWCOUNT = 0
    THROW 50002, 'Message not found, already processed, not exhausted, or currently leased.', 1;

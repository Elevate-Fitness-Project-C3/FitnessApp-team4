DECLARE @MaxAttempts int = 4;

SELECT Id, Purpose, Attempts, CreatedOn, NextAttemptOn, LockedUntil
FROM dbo.EmailOutbox
WHERE ProcessedOn IS NULL AND Attempts >= @MaxAttempts
ORDER BY Id;

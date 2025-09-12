-- Create the ToDos table
CREATE TABLE ToDos (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    IsCompleted BIT NOT NULL
);
GO

-- Stored Procedure: Add a new ToDo
CREATE PROCEDURE sp_AddToDo
    @Id UNIQUEIDENTIFIER,
    @Title NVARCHAR(255),
    @IsCompleted BIT
AS
BEGIN
    INSERT INTO ToDos (Id, Title, IsCompleted)
    VALUES (@Id, @Title, @IsCompleted);

    SELECT Id, Title, IsCompleted
    FROM ToDos
    WHERE Id = @Id;
END
GO

-- Stored Procedure: Delete a ToDo by Id
CREATE PROCEDURE sp_DeleteToDo
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    DELETE FROM ToDos WHERE Id = @Id;
    SELECT @@ROWCOUNT;
END
GO

-- Stored Procedure: Obtener todos los ToDos
CREATE PROCEDURE sp_GetAllToDos
AS
BEGIN
    SELECT Id, Title, IsCompleted FROM ToDos;
END
GO

-- Stored Procedure: Get a ToDo by Id
CREATE PROCEDURE sp_GetToDoById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SELECT Id, Title, IsCompleted
    FROM ToDos
    WHERE Id = @Id;
END
GO

-- Stored Procedure: Change the IsCompleted status of a ToDo
CREATE PROCEDURE sp_ToggleToDoStatus
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    UPDATE ToDos
    SET IsCompleted = ~IsCompleted
    WHERE Id = @Id;

    SELECT @@ROWCOUNT;
END
GO

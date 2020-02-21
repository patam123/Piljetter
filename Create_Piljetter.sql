--DROP DATABASE Piljetter

CREATE DATABASE Piljetter
USE Piljetter
-- Begränsningar för artister när det är skapat på detta sätt är att:
-- Det inte går att lägga in artister som har samma namn. Dessutom blir tabellen långsam för inserts.
-- Däremot fungerar det snabbare för sökningar. Vilket troligtvis kommer bli mer använt.
Create table Artist(
[Name] NVARCHAR(50) UNIQUE NOT NULL,
Popularity INT NOT NULL,
Primary key ([Name]),
CONSTRAINT CHK_Popularity CHECK(Popularity <= 100))

Create Table City(
Id INT IDENTITY (1,1) NOT NULL,
[Name] NVARCHAR(30) NOT NULL,
Country NVARCHAR(50) NOT NULL,
PRIMARY KEY (Id))

Create Table Venue (
Id INT IDENTITY(1,1) NOT NULL,
[Name] NVARCHAR(30) NOT NULL,
CityId INT NOT NULL,
Capacity INT NOT NULL,
Rating INT NOT NULL,
Primary key(Id),
FOREIGN KEY (CityId) REFERENCES City(Id),
CONSTRAINT CHK_Rating CHECK(Rating <= 50))

Create Table Concert (
Id INT IDENTITY(1,1) NOT NULL,
[Date] DATETIME2 NOT NULL,
ArtistName NVARCHAR(50) NOT NULL,
VenueId INT NOT NULL,
Price INT NOT NULL,
AvailableTickets INT NOT NULL,
Cost INT NOT NULL,
IsCancelled BIT NOT NULL,
PRIMARY KEY(Id),
FOREIGN KEY(ArtistName) REFERENCES Artist (Name),
FOREIGN KEY (VenueId) REFERENCES Venue(Id),
CONSTRAINT CHK_Date CHECK ([Date] > GETDATE()),
CONSTRAINT CHK_Tickets CHECK (AvailableTickets >= 0))

Create table Customer (
Id INT IDENTITY(1, 1) NOT NULL,
Email NVARCHAR(70) UNIQUE NOT NULL,
Balance INT NOT NULL,
Primary key(Id),
CONSTRAINT CHK_Balance CHECK(Balance >= 0))

--Här är det viktigt att inserts går fort så att kunden slipper vänta på att en biljett ska registreras.
--Det är därför väldigt smidigt att ha ID som primary key.
Create table Ticket(
Id INT IDENTITY (1,1) NOT NULL,
ConcertId INT NOT NULL,
Price INT NOT NULL,
PurchaseTime DATETIME2 NOT NULL,
CustomerId INT NOT NULL,
PRIMARY KEY (Id),
FOREIGN KEY (ConcertId) REFERENCES Concert(Id),
FOREIGN KEY (CustomerId) REFERENCES Customer(Id))

CREATE TABLE Coupon(
Id INT IDENTITY(1,1) NOT NULL,
CustomerId INT NOT NULL,
[ExpireDate] DATE NOT NULL,
PRIMARY KEY (Id),
FOREIGN KEY (CustomerId) REFERENCES Customer(Id))

CREATE TABLE RevokedTickets(
Id INT UNIQUE NOT NULL,
ConcertId INT NOT NULL,
Price INT NOT NULL,
PurchaseTime DATETIME2 NOT NULL,
CustomerId INT NOT NULL,
PRIMARY KEY (Id),
FOREIGN KEY (ConcertId) REFERENCES Concert(Id),
FOREIGN KEY (CustomerId) REFERENCES Customer(Id))


CREATE NONCLUSTERED INDEX concert_search ON Concert(ArtistName) INCLUDE (VenueId, [Date], IsCancelled)
CREATE NONCLUSTERED INDEX city_NameAndCountry ON City([Name]) INCLUDE (Country)
CREATE NONCLUSTERED INDEX venue_NameCityId ON Venue([Name]) INCLUDE (CityId)
CREATE NONCLUSTERED INDEX ticket_concertId ON Ticket(ConcertId) INCLUDE (CustomerId)
CREATE NONCLUSTERED INDEX revokedTicket_concertId ON RevokedTickets(ConcertId)

EXEC(
'CREATE VIEW [ConcertProfit] AS
SELECT c.Id, SUM(t.Price)-(c.Cost) AS Profit
FROM Concert c INNER JOIN Ticket t ON t.ConcertId = c.Id
GROUP BY c.Id, c.Cost;');

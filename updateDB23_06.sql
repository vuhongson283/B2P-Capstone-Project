-- Thêm trường Discount vào TimeSlot
ALTER TABLE TimeSlot
ADD Discount DECIMAL(5,2);

-- Thêm trường IsDayOff vào Booking
ALTER TABLE Booking
ADD IsDayOff BIT DEFAULT 0;

ALTER TABLE Facility 
ADD FacilityName NVARCHAR(255) NOT NULL DEFAULT 'Unknown Facility';
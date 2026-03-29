SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRAN;

    DECLARE @BeforeLinks INT = (SELECT COUNT(*) FROM dbo.MemberMemberships);
    DECLARE @BeforePayments INT = (SELECT COUNT(*) FROM dbo.Payments WHERE MemberMembershipId IS NOT NULL);

    DELETE FROM dbo.Payments WHERE MemberMembershipId IS NOT NULL;

    IF OBJECT_ID('dbo.InvoiceSnapshots', 'U') IS NOT NULL
    BEGIN
        DELETE FROM dbo.InvoiceSnapshots WHERE MemberMembershipId IS NOT NULL;
    END

    DELETE FROM dbo.MemberMemberships;

    IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'arjun.kapoor.mock@gym.local')
    INSERT INTO dbo.Members (Id, UserId, Name, DateOfBirth, Gender, Phone, Email, AddressLine, City, State, Pincode, Height, Weight, JoiningDate, MembershipStatus, AssignedTrainerId, ProfilePhotoPath)
    VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', 'Arjun Kapoor', '1998-06-12', 'Male', '9876501001', 'arjun.kapoor.mock@gym.local', '12 Lake View', 'Mumbai', 'Maharashtra', '400001', 178, 76, '2023-01-10T00:00:00', 'Active', NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'kavya.sharma.mock@gym.local')
    INSERT INTO dbo.Members (Id, UserId, Name, DateOfBirth, Gender, Phone, Email, AddressLine, City, State, Pincode, Height, Weight, JoiningDate, MembershipStatus, AssignedTrainerId, ProfilePhotoPath)
    VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', 'Kavya Sharma', '1999-02-03', 'Female', '9876501002', 'kavya.sharma.mock@gym.local', '44 Green Park', 'Delhi', 'Delhi', '110001', 162, 58, '2023-02-11T00:00:00', 'Active', NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'rahul.mehta.mock@gym.local')
    INSERT INTO dbo.Members (Id, UserId, Name, DateOfBirth, Gender, Phone, Email, AddressLine, City, State, Pincode, Height, Weight, JoiningDate, MembershipStatus, AssignedTrainerId, ProfilePhotoPath)
    VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', 'Rahul Mehta', '1995-09-21', 'Male', '9876501003', 'rahul.mehta.mock@gym.local', '90 Sunrise Street', 'Pune', 'Maharashtra', '411001', 174, 81, '2023-03-15T00:00:00', 'Active', NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'neha.iyer.mock@gym.local')
    INSERT INTO dbo.Members (Id, UserId, Name, DateOfBirth, Gender, Phone, Email, AddressLine, City, State, Pincode, Height, Weight, JoiningDate, MembershipStatus, AssignedTrainerId, ProfilePhotoPath)
    VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', 'Neha Iyer', '1997-12-18', 'Female', '9876501004', 'neha.iyer.mock@gym.local', '15 Temple Road', 'Chennai', 'Tamil Nadu', '600001', 160, 55, '2023-04-05T00:00:00', 'Active', NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'amit.verma.mock@gym.local')
    INSERT INTO dbo.Members (Id, UserId, Name, DateOfBirth, Gender, Phone, Email, AddressLine, City, State, Pincode, Height, Weight, JoiningDate, MembershipStatus, AssignedTrainerId, ProfilePhotoPath)
    VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', 'Amit Verma', '1992-11-07', 'Male', '9876501005', 'amit.verma.mock@gym.local', '221 Park Lane', 'Bengaluru', 'Karnataka', '560001', 180, 84, '2024-01-12T00:00:00', 'Active', NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'sneha.reddy.mock@gym.local')
    INSERT INTO dbo.Members (Id, UserId, Name, DateOfBirth, Gender, Phone, Email, AddressLine, City, State, Pincode, Height, Weight, JoiningDate, MembershipStatus, AssignedTrainerId, ProfilePhotoPath)
    VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', 'Sneha Reddy', '1994-03-30', 'Female', '9876501006', 'sneha.reddy.mock@gym.local', '7 Orchid Enclave', 'Hyderabad', 'Telangana', '500001', 164, 59, '2024-02-20T00:00:00', 'Active', NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'rohan.patel.mock@gym.local')
    INSERT INTO dbo.Members (Id, UserId, Name, DateOfBirth, Gender, Phone, Email, AddressLine, City, State, Pincode, Height, Weight, JoiningDate, MembershipStatus, AssignedTrainerId, ProfilePhotoPath)
    VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', 'Rohan Patel', '1996-08-17', 'Male', '9876501007', 'rohan.patel.mock@gym.local', '31 River Side', 'Ahmedabad', 'Gujarat', '380001', 176, 79, '2024-06-10T00:00:00', 'Active', NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'priya.nair.mock@gym.local')
    INSERT INTO dbo.Members (Id, UserId, Name, DateOfBirth, Gender, Phone, Email, AddressLine, City, State, Pincode, Height, Weight, JoiningDate, MembershipStatus, AssignedTrainerId, ProfilePhotoPath)
    VALUES (NEWID(), '00000000-0000-0000-0000-000000000000', 'Priya Nair', '1993-05-25', 'Female', '9876501008', 'priya.nair.mock@gym.local', '18 Hill Top', 'Kochi', 'Kerala', '682001', 158, 54, '2024-07-08T00:00:00', 'Active', NULL, NULL);

    DECLARE @M1 UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Members WHERE Email = 'arjun.kapoor.mock@gym.local');
    DECLARE @M2 UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Members WHERE Email = 'kavya.sharma.mock@gym.local');
    DECLARE @M3 UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Members WHERE Email = 'rahul.mehta.mock@gym.local');
    DECLARE @M4 UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Members WHERE Email = 'neha.iyer.mock@gym.local');
    DECLARE @M5 UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Members WHERE Email = 'amit.verma.mock@gym.local');
    DECLARE @M6 UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Members WHERE Email = 'sneha.reddy.mock@gym.local');
    DECLARE @M7 UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Members WHERE Email = 'rohan.patel.mock@gym.local');
    DECLARE @M8 UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Members WHERE Email = 'priya.nair.mock@gym.local');

    DECLARE @P1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.MembershipPlans WHERE PlanName = 'Basic - 1 Month');
    DECLARE @P6 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.MembershipPlans WHERE PlanName = 'Pro - 6 Months');
    DECLARE @PC UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.MembershipPlans WHERE MembershipType = 'Couple' ORDER BY Price DESC);

    IF @P1 IS NULL OR @P6 IS NULL OR @PC IS NULL
    BEGIN
        THROW 50001, 'Required plans missing. Ensure Basic - 1 Month, Pro - 6 Months, and a Couple plan exist.', 1;
    END

    DECLARE @Price1 DECIMAL(18,2) = (SELECT Price FROM dbo.MembershipPlans WHERE Id = @P1);
    DECLARE @Price6 DECIMAL(18,2) = (SELECT Price FROM dbo.MembershipPlans WHERE Id = @P6);
    DECLARE @PriceC DECIMAL(18,2) = (SELECT Price FROM dbo.MembershipPlans WHERE Id = @PC);

    DECLARE @MM1 UNIQUEIDENTIFIER = NEWID();
    DECLARE @MM2 UNIQUEIDENTIFIER = NEWID();
    DECLARE @MM3 UNIQUEIDENTIFIER = NEWID();
    DECLARE @MM4 UNIQUEIDENTIFIER = NEWID();
    DECLARE @MM5 UNIQUEIDENTIFIER = NEWID();
    DECLARE @MM6 UNIQUEIDENTIFIER = NEWID();
    DECLARE @MM7 UNIQUEIDENTIFIER = NEWID();
    DECLARE @MM8 UNIQUEIDENTIFIER = NEWID();
    DECLARE @MM9 UNIQUEIDENTIFIER = NEWID();

    INSERT INTO dbo.MemberMemberships (Id, MemberId, SecondaryMemberId, MembershipPlanId, MasterInvoiceNumber, CreatedOn, PlanPriceAtEnrollment, Discount, Description, StartDate, EndDate, IsActive)
    VALUES
    (@MM1, @M1, NULL, @P1, 'INV-MOCK-2023-001', '2023-03-01T10:00:00', @Price1, 0, '1-month starter plan', '2023-03-01', '2023-04-01', 0),
    (@MM2, @M2, NULL, @P6, 'INV-MOCK-2023-002', '2023-06-10T12:15:00', @Price6, 200, '6-month discounted', '2023-06-10', '2023-12-10', 0),
    (@MM3, @M3, @M4, @PC, 'INV-MOCK-2023-003', '2023-09-05T09:45:00', @PriceC, 500, 'Couple annual offer', '2023-09-05', '2024-09-05', 0),
    (@MM4, @M5, NULL, @P6, 'INV-MOCK-2024-004', '2024-02-20T11:00:00', @Price6, 0, '6-month standard', '2024-02-20', '2024-08-20', 0),
    (@MM5, @M6, NULL, @P1, 'INV-MOCK-2024-005', '2024-11-01T08:30:00', @Price1, 0, '1-month renewal', '2024-11-01', '2024-12-01', 0),
    (@MM6, @M7, @M8, @PC, 'INV-MOCK-2025-006', '2025-01-15T17:20:00', @PriceC, 1000, 'Couple promo', '2025-01-15', '2026-01-15', 0),
    (@MM7, @M1, NULL, @P6, 'INV-MOCK-2025-007', '2025-05-01T10:10:00', @Price6, 250, '6-month repeat', '2025-05-01', '2025-11-01', 0),
    (@MM8, @M2, NULL, @P1, 'INV-MOCK-2026-008', '2026-01-10T09:00:00', @Price1, 0, 'Current year monthly', '2026-01-10', '2026-02-10', 0),
    (@MM9, @M5, NULL, @P6, 'INV-MOCK-2026-009', '2026-02-01T14:00:00', @Price6, 300, 'Current active 6-month', '2026-02-01', '2026-08-01', 1);

    INSERT INTO dbo.Payments (Id, MemberId, MemberMembershipId, Amount, PaidOn, PaymentMode, TransactionReference, Notes, InvoiceNumber, ReceiptNumber)
    VALUES
    (NEWID(), @M1, @MM1, @Price1, '2023-03-01T10:30:00', 'Cash', 'CASH-230301', 'Full payment', 'INV-MOCK-2023-001', 'REC-MOCK-230301-01'),

    (NEWID(), @M2, @MM2, 1200, '2023-06-10T13:00:00', 'Upi', 'UPI-230610', 'Part 1', 'INV-MOCK-2023-002', 'REC-MOCK-230610-01'),
    (NEWID(), @M2, @MM2, @Price6 - 200 - 1200, '2023-08-15T09:00:00', 'Card', 'CARD-230815', 'Part 2', 'INV-MOCK-2023-002', 'REC-MOCK-230815-01'),

    (NEWID(), @M3, @MM3, 4000, '2023-09-05T10:15:00', 'Cash', 'CASH-230905', 'Partial payment', 'INV-MOCK-2023-003', 'REC-MOCK-230905-01'),

    (NEWID(), @M5, @MM4, @Price6, '2024-02-20T11:30:00', 'Upi', 'UPI-240220', 'Full payment', 'INV-MOCK-2024-004', 'REC-MOCK-240220-01'),

    (NEWID(), @M6, @MM5, @Price1, '2024-11-01T09:00:00', 'Cash', 'CASH-241101', 'Full payment', 'INV-MOCK-2024-005', 'REC-MOCK-241101-01'),

    (NEWID(), @M7, @MM6, 5000, '2025-01-15T17:45:00', 'Card', 'CARD-250115', 'Installment 1', 'INV-MOCK-2025-006', 'REC-MOCK-250115-01'),
    (NEWID(), @M7, @MM6, 3500, '2025-03-01T10:20:00', 'Upi', 'UPI-250301', 'Installment 2', 'INV-MOCK-2025-006', 'REC-MOCK-250301-01'),

    (NEWID(), @M1, @MM7, @Price6 - 250, '2025-05-01T11:00:00', 'Cash', 'CASH-250501', 'Full payment', 'INV-MOCK-2025-007', 'REC-MOCK-250501-01'),

    (NEWID(), @M2, @MM8, @Price1, '2026-01-10T10:00:00', 'Upi', 'UPI-260110', 'Full payment', 'INV-MOCK-2026-008', 'REC-MOCK-260110-01'),

    (NEWID(), @M5, @MM9, 2000, '2026-02-01T14:20:00', 'Card', 'CARD-260201', 'Initial partial', 'INV-MOCK-2026-009', 'REC-MOCK-260201-01');

    UPDATE dbo.MemberMemberships
    SET IsActive = CASE WHEN EndDate < CAST(GETUTCDATE() AS date) THEN 0 ELSE IsActive END;

    DECLARE @AfterLinks INT = (SELECT COUNT(*) FROM dbo.MemberMemberships);
    DECLARE @AfterPayments INT = (SELECT COUNT(*) FROM dbo.Payments WHERE MemberMembershipId IS NOT NULL);

    COMMIT;

    SELECT
        @BeforeLinks AS BeforeLinks,
        @BeforePayments AS BeforeMembershipPayments,
        @AfterLinks AS AfterLinks,
        @AfterPayments AS AfterMembershipPayments;

    SELECT TOP 20
        mm.CreatedOn,
        m.Name AS PrimaryMember,
        ISNULL(sm.Name, '-') AS SecondaryMember,
        mp.PlanName,
        mp.MembershipType,
        mm.PlanPriceAtEnrollment,
        mm.Discount,
        mm.StartDate,
        mm.EndDate,
        mm.IsActive
    FROM dbo.MemberMemberships mm
    JOIN dbo.Members m ON m.Id = mm.MemberId
    LEFT JOIN dbo.Members sm ON sm.Id = mm.SecondaryMemberId
    JOIN dbo.MembershipPlans mp ON mp.Id = mm.MembershipPlanId
    ORDER BY mm.CreatedOn DESC;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH

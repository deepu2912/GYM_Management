using GymManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await EnsureManualSchemaChangesAsync(context);

        if (!await context.MembershipPlans.AnyAsync())
        {
            context.MembershipPlans.AddRange(
                new MembershipPlan
                {
                    Id = Guid.NewGuid(),
                    PlanName = "Basic - 1 Month",
                    MembershipType = MembershipType.Single,
                    Duration = MembershipDuration.OneMonth,
                    Price = 999,
                    Description = "Starter monthly plan."
                },
                new MembershipPlan
                {
                    Id = Guid.NewGuid(),
                    PlanName = "Standard - 3 Months",
                    MembershipType = MembershipType.Single,
                    Duration = MembershipDuration.ThreeMonths,
                    Price = 2499,
                    Description = "Quarterly plan with better value."
                },
                new MembershipPlan
                {
                    Id = Guid.NewGuid(),
                    PlanName = "Pro - 6 Months",
                    MembershipType = MembershipType.Single,
                    Duration = MembershipDuration.SixMonths,
                    Price = 4499,
                    Description = "Half-year plan."
                },
                new MembershipPlan
                {
                    Id = Guid.NewGuid(),
                    PlanName = "Elite - 1 Year",
                    MembershipType = MembershipType.Single,
                    Duration = MembershipDuration.OneYear,
                    Price = 7999,
                    Description = "Annual membership plan."
                });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureManualSchemaChangesAsync(AppDbContext context)
    {
        const string dropAssignedPlanColumnSql = """
            IF COL_LENGTH('dbo.Members', 'AssignedPlan') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Members] DROP COLUMN [AssignedPlan];
            END
            """;

        const string createMembershipPlansSql = """
            IF OBJECT_ID('dbo.MembershipPlans', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[MembershipPlans](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [GymTenantId] uniqueidentifier NULL,
                    [PlanName] nvarchar(450) NOT NULL,
                    [MembershipType] nvarchar(max) NOT NULL,
                    [Duration] nvarchar(max) NOT NULL,
                    [Price] decimal(18,2) NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [IsActive] bit NOT NULL
                );
                CREATE UNIQUE INDEX [IX_MembershipPlans_GymTenantId_PlanName] ON [dbo].[MembershipPlans]([GymTenantId], [PlanName]);
            END
            """;

        const string patchMembershipPlansTableSql = """
            IF COL_LENGTH('dbo.MembershipPlans', 'MembershipType') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MembershipPlans] ADD [MembershipType] nvarchar(max) NOT NULL CONSTRAINT [DF_MembershipPlans_MembershipType] DEFAULT 'Single';
            END

            IF COL_LENGTH('dbo.MembershipPlans', 'IsActive') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MembershipPlans] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_MembershipPlans_IsActive] DEFAULT 1;
            END

            IF COL_LENGTH('dbo.MembershipPlans', 'GymTenantId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MembershipPlans] ADD [GymTenantId] uniqueidentifier NULL;
            END

            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_MembershipPlans_PlanName'
                AND object_id = OBJECT_ID('dbo.MembershipPlans')
            )
            BEGIN
                DROP INDEX [IX_MembershipPlans_PlanName] ON [dbo].[MembershipPlans];
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_MembershipPlans_GymTenantId_PlanName'
                AND object_id = OBJECT_ID('dbo.MembershipPlans')
            )
            BEGIN
                CREATE UNIQUE INDEX [IX_MembershipPlans_GymTenantId_PlanName]
                ON [dbo].[MembershipPlans]([GymTenantId], [PlanName]);
            END
            """;

        const string createMemberMembershipsSql = """
            IF OBJECT_ID('dbo.MemberMemberships', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[MemberMemberships](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [MemberId] uniqueidentifier NOT NULL,
                    [SecondaryMemberId] uniqueidentifier NULL,
                    [MembershipPlanId] uniqueidentifier NOT NULL,
                    [MasterInvoiceNumber] nvarchar(450) NULL,
                    [CreatedOn] datetime2 NOT NULL,
                    [PlanPriceAtEnrollment] decimal(18,2) NOT NULL,
                    [Discount] decimal(18,2) NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [StartDate] date NOT NULL,
                    [EndDate] date NOT NULL,
                    [IsActive] bit NOT NULL,
                    CONSTRAINT [FK_MemberMemberships_Members_MemberId]
                        FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Members]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_MemberMemberships_Members_SecondaryMemberId]
                        FOREIGN KEY ([SecondaryMemberId]) REFERENCES [dbo].[Members]([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_MemberMemberships_MembershipPlans_MembershipPlanId]
                        FOREIGN KEY ([MembershipPlanId]) REFERENCES [dbo].[MembershipPlans]([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_MemberMemberships_MemberId] ON [dbo].[MemberMemberships]([MemberId]);
                CREATE INDEX [IX_MemberMemberships_SecondaryMemberId] ON [dbo].[MemberMemberships]([SecondaryMemberId]);
                CREATE INDEX [IX_MemberMemberships_MembershipPlanId] ON [dbo].[MemberMemberships]([MembershipPlanId]);
            END
            """;

        const string patchMemberMembershipsTableSql = """
            IF COL_LENGTH('dbo.MemberMemberships', 'SecondaryMemberId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MemberMemberships] ADD [SecondaryMemberId] uniqueidentifier NULL;
            END

            IF COL_LENGTH('dbo.MemberMemberships', 'Discount') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MemberMemberships] ADD [Discount] decimal(18,2) NOT NULL CONSTRAINT [DF_MemberMemberships_Discount] DEFAULT 0;
            END

            IF COL_LENGTH('dbo.MemberMemberships', 'Description') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MemberMemberships] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_MemberMemberships_Description] DEFAULT '';
            END

            IF COL_LENGTH('dbo.MemberMemberships', 'MasterInvoiceNumber') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MemberMemberships] ADD [MasterInvoiceNumber] nvarchar(450) NULL;
            END

            IF COL_LENGTH('dbo.MemberMemberships', 'CreatedOn') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MemberMemberships]
                ADD [CreatedOn] datetime2 NOT NULL CONSTRAINT [DF_MemberMemberships_CreatedOn] DEFAULT SYSUTCDATETIME();
            END

            IF COL_LENGTH('dbo.MemberMemberships', 'PlanPriceAtEnrollment') IS NULL
            BEGIN
                EXEC('ALTER TABLE [dbo].[MemberMemberships] ADD [PlanPriceAtEnrollment] decimal(18,2) NULL;');

                EXEC('
                    UPDATE mm
                    SET mm.[PlanPriceAtEnrollment] = mp.[Price]
                    FROM [dbo].[MemberMemberships] mm
                    INNER JOIN [dbo].[MembershipPlans] mp ON mm.[MembershipPlanId] = mp.[Id]
                    WHERE mm.[PlanPriceAtEnrollment] IS NULL;
                ');

                EXEC('ALTER TABLE [dbo].[MemberMemberships] ALTER COLUMN [PlanPriceAtEnrollment] decimal(18,2) NOT NULL;');
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_MemberMemberships_SecondaryMemberId'
                AND object_id = OBJECT_ID('dbo.MemberMemberships')
            )
            BEGIN
                CREATE INDEX [IX_MemberMemberships_SecondaryMemberId] ON [dbo].[MemberMemberships]([SecondaryMemberId]);
            END

            IF NOT EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE name = 'FK_MemberMemberships_Members_SecondaryMemberId'
            )
            BEGIN
                ALTER TABLE [dbo].[MemberMemberships]
                ADD CONSTRAINT [FK_MemberMemberships_Members_SecondaryMemberId]
                FOREIGN KEY ([SecondaryMemberId]) REFERENCES [dbo].[Members]([Id]) ON DELETE NO ACTION;
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_MemberMemberships_MemberId_StartDate_EndDate'
                AND object_id = OBJECT_ID('dbo.MemberMemberships')
            )
            BEGIN
                CREATE INDEX [IX_MemberMemberships_MemberId_StartDate_EndDate]
                ON [dbo].[MemberMemberships]([MemberId], [StartDate], [EndDate]);
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_MemberMemberships_SecondaryMemberId_StartDate_EndDate'
                AND object_id = OBJECT_ID('dbo.MemberMemberships')
            )
            BEGIN
                CREATE INDEX [IX_MemberMemberships_SecondaryMemberId_StartDate_EndDate]
                ON [dbo].[MemberMemberships]([SecondaryMemberId], [StartDate], [EndDate]);
            END
            """;

        const string patchMembersTableSql = """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_Members_JoiningDate'
                AND object_id = OBJECT_ID('dbo.Members')
            )
            BEGIN
                CREATE INDEX [IX_Members_JoiningDate] ON [dbo].[Members]([JoiningDate]);
            END
            """;

        const string createTrainerProfilesSql = """
            IF OBJECT_ID('dbo.TrainerProfiles', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[TrainerProfiles](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [UserId] uniqueidentifier NOT NULL,
                    [Phone] nvarchar(max) NOT NULL,
                    [Address] nvarchar(max) NOT NULL,
                    [Specialization] nvarchar(max) NOT NULL,
                    [BaseSalary] decimal(18,2) NOT NULL,
                    [JoiningDate] datetime2 NOT NULL,
                    [IsActive] bit NOT NULL,
                    CONSTRAINT [FK_TrainerProfiles_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_TrainerProfiles_UserId] ON [dbo].[TrainerProfiles]([UserId]);
            END
            """;

        const string createTrainerSalariesSql = """
            IF OBJECT_ID('dbo.TrainerSalaries', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[TrainerSalaries](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [TrainerUserId] uniqueidentifier NOT NULL,
                    [Year] int NOT NULL,
                    [Month] int NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [IsPaid] bit NOT NULL,
                    [PaidOn] datetime2 NULL,
                    [Remarks] nvarchar(max) NOT NULL,
                    CONSTRAINT [FK_TrainerSalaries_Users_TrainerUserId]
                        FOREIGN KEY ([TrainerUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_TrainerSalaries_TrainerUserId_Year_Month]
                    ON [dbo].[TrainerSalaries]([TrainerUserId], [Year], [Month]);
            END
            """;

        const string createPaymentsSql = """
            IF OBJECT_ID('dbo.Payments', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Payments](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [MemberId] uniqueidentifier NOT NULL,
                    [MemberMembershipId] uniqueidentifier NULL,
                    [ReceiptNumber] nvarchar(450) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [PaidOn] datetime2 NOT NULL,
                    [PaymentMode] nvarchar(max) NOT NULL,
                    [TransactionReference] nvarchar(max) NULL,
                    [Notes] nvarchar(max) NULL,
                    [InvoiceNumber] nvarchar(450) NOT NULL,
                    CONSTRAINT [FK_Payments_Members_MemberId]
                        FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Members]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_Payments_MemberMemberships_MemberMembershipId]
                        FOREIGN KEY ([MemberMembershipId]) REFERENCES [dbo].[MemberMemberships]([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Payments_InvoiceNumber] ON [dbo].[Payments]([InvoiceNumber]);
                CREATE UNIQUE INDEX [IX_Payments_ReceiptNumber] ON [dbo].[Payments]([ReceiptNumber]);
                CREATE INDEX [IX_Payments_MemberId] ON [dbo].[Payments]([MemberId]);
            END
            """;

        const string patchPaymentsTableSql = """
            IF COL_LENGTH('dbo.Payments', 'ReceiptNumber') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Payments] ADD [ReceiptNumber] nvarchar(450) NULL;
            END

            IF COL_LENGTH('dbo.Payments', 'ReceiptNumber') IS NOT NULL
            BEGIN
                EXEC('
                    UPDATE [dbo].[Payments]
                    SET [ReceiptNumber] = CONCAT(''REC-LEGACY-'', REPLACE(CONVERT(nvarchar(36), [Id]), ''-'', ''''))
                    WHERE [ReceiptNumber] IS NULL;
                ');
            END

            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_Payments_InvoiceNumber'
                AND object_id = OBJECT_ID('dbo.Payments')
                AND is_unique = 1
            )
            BEGIN
                DROP INDEX [IX_Payments_InvoiceNumber] ON [dbo].[Payments];
                CREATE INDEX [IX_Payments_InvoiceNumber] ON [dbo].[Payments]([InvoiceNumber]);
            END

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_Payments_ReceiptNumber'
                AND object_id = OBJECT_ID('dbo.Payments')
            )
            BEGIN
                IF COL_LENGTH('dbo.Payments', 'ReceiptNumber') IS NOT NULL
                BEGIN
                    EXEC('
                        CREATE UNIQUE INDEX [IX_Payments_ReceiptNumber]
                        ON [dbo].[Payments]([ReceiptNumber])
                        WHERE [ReceiptNumber] IS NOT NULL;
                    ');
                END
            END
            """;

        const string createInvoiceSnapshotsSql = """
            IF OBJECT_ID('dbo.InvoiceSnapshots', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[InvoiceSnapshots](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [InvoiceNumber] nvarchar(450) NOT NULL,
                    [MemberId] uniqueidentifier NOT NULL,
                    [MemberMembershipId] uniqueidentifier NULL,
                    [InvoiceDate] datetime2 NOT NULL,
                    [SnapshotJson] nvarchar(max) NOT NULL,
                    [CreatedOn] datetime2 NOT NULL,
                    CONSTRAINT [FK_InvoiceSnapshots_Members_MemberId]
                        FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Members]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_InvoiceSnapshots_InvoiceNumber] ON [dbo].[InvoiceSnapshots]([InvoiceNumber]);
                CREATE INDEX [IX_InvoiceSnapshots_MemberId] ON [dbo].[InvoiceSnapshots]([MemberId]);
            END
            """;

        const string createAttendancesSql = """
            IF OBJECT_ID('dbo.MemberAttendances', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[MemberAttendances](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [MemberId] uniqueidentifier NOT NULL,
                    [AttendanceDate] date NOT NULL,
                    [Status] nvarchar(max) NOT NULL,
                    [CheckInTime] time NULL,
                    [Notes] nvarchar(max) NULL,
                    CONSTRAINT [FK_MemberAttendances_Members_MemberId]
                        FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Members]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_MemberAttendances_MemberId_AttendanceDate]
                    ON [dbo].[MemberAttendances]([MemberId], [AttendanceDate]);
            END
            """;

        const string createGymProfileSql = """
            IF OBJECT_ID('dbo.GymProfiles', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GymProfiles](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [GymName] nvarchar(max) NOT NULL,
                    [Email] nvarchar(450) NOT NULL,
                    [Phone] nvarchar(max) NOT NULL,
                    [AddressLine] nvarchar(max) NOT NULL,
                    [City] nvarchar(max) NOT NULL,
                    [State] nvarchar(max) NOT NULL,
                    [Pincode] nvarchar(max) NOT NULL,
                    [GstNumber] nvarchar(max) NOT NULL,
                    [BankName] nvarchar(max) NOT NULL,
                    [AccountName] nvarchar(max) NOT NULL,
                    [AccountNumber] nvarchar(max) NOT NULL,
                    [IfscCode] nvarchar(max) NOT NULL,
                    [UpiId] nvarchar(max) NULL,
                    [LogoDataUri] nvarchar(max) NULL,
                    [HsnSacCode] nvarchar(50) NOT NULL,
                    [GstRatePercent] decimal(5,2) NOT NULL,
                    [IsGstApplicable] bit NOT NULL
                );
                CREATE UNIQUE INDEX [IX_GymProfiles_Email] ON [dbo].[GymProfiles]([Email]);
            END
            """;

        const string patchGymProfilesTableSql = """
            IF COL_LENGTH('dbo.GymProfiles', 'IsGstApplicable') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GymProfiles]
                ADD [IsGstApplicable] bit NOT NULL CONSTRAINT [DF_GymProfiles_IsGstApplicable] DEFAULT 1;
            END

            IF COL_LENGTH('dbo.GymProfiles', 'LogoDataUri') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GymProfiles]
                ADD [LogoDataUri] nvarchar(max) NULL;
            END

            IF COL_LENGTH('dbo.GymProfiles', 'HsnSacCode') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GymProfiles]
                ADD [HsnSacCode] nvarchar(50) NOT NULL CONSTRAINT [DF_GymProfiles_HsnSacCode] DEFAULT '9997';
            END

            IF COL_LENGTH('dbo.GymProfiles', 'GstRatePercent') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GymProfiles]
                ADD [GstRatePercent] decimal(5,2) NOT NULL CONSTRAINT [DF_GymProfiles_GstRatePercent] DEFAULT 18;
            END
            """;

        const string createGymTenantsSql = """
            IF OBJECT_ID('dbo.GymTenants', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GymTenants](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [GymName] nvarchar(450) NOT NULL,
                    [Email] nvarchar(450) NOT NULL,
                    [Phone] nvarchar(50) NOT NULL,
                    [AddressLine] nvarchar(250) NOT NULL,
                    [City] nvarchar(100) NOT NULL,
                    [State] nvarchar(100) NOT NULL,
                    [Pincode] nvarchar(20) NOT NULL,
                    [GstNumber] nvarchar(50) NOT NULL,
                    [BankName] nvarchar(100) NOT NULL,
                    [AccountName] nvarchar(100) NOT NULL,
                    [AccountNumber] nvarchar(50) NOT NULL,
                    [IfscCode] nvarchar(20) NOT NULL,
                    [UpiId] nvarchar(100) NULL,
                    [HsnSacCode] nvarchar(50) NOT NULL,
                    [GstRatePercent] decimal(5,2) NOT NULL,
                    [IsGstApplicable] bit NOT NULL,
                    [IsActive] bit NOT NULL,
                    [SubscriptionPlan] nvarchar(30) NOT NULL,
                    [SubscriptionValidTill] datetime2 NULL,
                    [LifetimePlanActivated] bit NOT NULL,
                    [CreatedOn] datetime2 NOT NULL
                );
                CREATE UNIQUE INDEX [IX_GymTenants_GymName] ON [dbo].[GymTenants]([GymName]);
                CREATE UNIQUE INDEX [IX_GymTenants_Email] ON [dbo].[GymTenants]([Email]);
            END
            """;

        const string patchGymTenantsSubscriptionSql = """
            IF COL_LENGTH('dbo.GymTenants', 'SubscriptionPlan') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GymTenants]
                ADD [SubscriptionPlan] nvarchar(30) NOT NULL CONSTRAINT [DF_GymTenants_SubscriptionPlan] DEFAULT 'None';
            END

            IF COL_LENGTH('dbo.GymTenants', 'SubscriptionValidTill') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GymTenants] ADD [SubscriptionValidTill] datetime2 NULL;
            END

            IF COL_LENGTH('dbo.GymTenants', 'LifetimePlanActivated') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GymTenants]
                ADD [LifetimePlanActivated] bit NOT NULL CONSTRAINT [DF_GymTenants_LifetimePlanActivated] DEFAULT 0;
            END
            """;

        const string unlinkLifetimeSubscriptionSql = """
            UPDATE [dbo].[GymTenants]
            SET
                [SubscriptionPlan] = 'None',
                [SubscriptionValidTill] = NULL,
                [LifetimePlanActivated] = 0
            WHERE
                [LifetimePlanActivated] = 1
                OR [SubscriptionPlan] IN ('Lifetime', 'LifetimeMaintenance');
            """;

        const string createGymSubscriptionPaymentsSql = """
            IF OBJECT_ID('dbo.GymSubscriptionPayments', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GymSubscriptionPayments](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [GymTenantId] uniqueidentifier NOT NULL,
                    [PlanCode] nvarchar(50) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [PaidOn] datetime2 NOT NULL,
                    [PaymentMode] nvarchar(50) NOT NULL,
                    [TransactionReference] nvarchar(200) NULL,
                    [Notes] nvarchar(max) NULL,
                    [InvoiceNumber] nvarchar(450) NOT NULL,
                    [CreatedOn] datetime2 NOT NULL,
                    CONSTRAINT [FK_GymSubscriptionPayments_GymTenants_GymTenantId]
                        FOREIGN KEY ([GymTenantId]) REFERENCES [dbo].[GymTenants]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_GymSubscriptionPayments_InvoiceNumber] ON [dbo].[GymSubscriptionPayments]([InvoiceNumber]);
                CREATE INDEX [IX_GymSubscriptionPayments_GymTenantId] ON [dbo].[GymSubscriptionPayments]([GymTenantId]);
            END
            """;

        const string createSubscriptionPlansSql = """
            IF OBJECT_ID('dbo.SubscriptionPlans', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SubscriptionPlans](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [Code] nvarchar(50) NOT NULL,
                    [Name] nvarchar(120) NOT NULL,
                    [Price] decimal(18,2) NOT NULL,
                    [DurationMonths] int NOT NULL,
                    [IsLifetime] bit NOT NULL,
                    [IsMaintenance] bit NOT NULL,
                    [IsActive] bit NOT NULL,
                    [SortOrder] int NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [CreatedOn] datetime2 NOT NULL,
                    [UpdatedOn] datetime2 NOT NULL
                );
                CREATE UNIQUE INDEX [IX_SubscriptionPlans_Code] ON [dbo].[SubscriptionPlans]([Code]);
            END
            """;

        const string seedSubscriptionPlansSql = """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[SubscriptionPlans] WHERE [Code] = 'SIXMONTHS')
            BEGIN
                INSERT INTO [dbo].[SubscriptionPlans]
                ([Id], [Code], [Name], [Price], [DurationMonths], [IsLifetime], [IsMaintenance], [IsActive], [SortOrder], [Description], [CreatedOn], [UpdatedOn])
                VALUES (NEWID(), 'SIXMONTHS', '6 Monthly', 3000, 6, 0, 0, 1, 10, '6 month subscription plan', SYSUTCDATETIME(), SYSUTCDATETIME());
            END

            IF NOT EXISTS (SELECT 1 FROM [dbo].[SubscriptionPlans] WHERE [Code] = 'YEARLY')
            BEGIN
                INSERT INTO [dbo].[SubscriptionPlans]
                ([Id], [Code], [Name], [Price], [DurationMonths], [IsLifetime], [IsMaintenance], [IsActive], [SortOrder], [Description], [CreatedOn], [UpdatedOn])
                VALUES (NEWID(), 'YEARLY', '1 Yearly', 5000, 12, 0, 0, 1, 20, '12 month subscription plan', SYSUTCDATETIME(), SYSUTCDATETIME());
            END

            IF NOT EXISTS (SELECT 1 FROM [dbo].[SubscriptionPlans] WHERE [Code] = 'LIFETIME')
            BEGIN
                INSERT INTO [dbo].[SubscriptionPlans]
                ([Id], [Code], [Name], [Price], [DurationMonths], [IsLifetime], [IsMaintenance], [IsActive], [SortOrder], [Description], [CreatedOn], [UpdatedOn])
                VALUES (NEWID(), 'LIFETIME', 'Lifetime', 12000, 12, 1, 0, 1, 30, 'Lifetime activation with yearly validity tracking', SYSUTCDATETIME(), SYSUTCDATETIME());
            END

            IF NOT EXISTS (SELECT 1 FROM [dbo].[SubscriptionPlans] WHERE [Code] = 'LIFETIMEMAINTENANCE')
            BEGIN
                INSERT INTO [dbo].[SubscriptionPlans]
                ([Id], [Code], [Name], [Price], [DurationMonths], [IsLifetime], [IsMaintenance], [IsActive], [SortOrder], [Description], [CreatedOn], [UpdatedOn])
                VALUES (NEWID(), 'LIFETIMEMAINTENANCE', 'Maintenance', 2000, 12, 0, 1, 1, 40, 'Yearly maintenance for lifetime plan', SYSUTCDATETIME(), SYSUTCDATETIME());
            END
            """;

        const string createSuperAdminInvoiceSettingsSql = """
            IF OBJECT_ID('dbo.SuperAdminInvoiceSettings', 'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SuperAdminInvoiceSettings](
                    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                    [BusinessName] nvarchar(200) NOT NULL,
                    [Email] nvarchar(200) NOT NULL,
                    [Phone] nvarchar(20) NOT NULL,
                    [AddressLine] nvarchar(300) NOT NULL,
                    [City] nvarchar(100) NOT NULL,
                    [State] nvarchar(100) NOT NULL,
                    [Pincode] nvarchar(20) NOT NULL,
                    [GstNumber] nvarchar(50) NOT NULL,
                    [BankName] nvarchar(100) NOT NULL,
                    [AccountName] nvarchar(100) NOT NULL,
                    [AccountNumber] nvarchar(50) NOT NULL,
                    [IfscCode] nvarchar(20) NOT NULL,
                    [UpiId] nvarchar(100) NULL,
                    [AuthorizedSignatory] nvarchar(120) NOT NULL,
                    [TermsAndConditions] nvarchar(max) NOT NULL,
                    [CreatedOn] datetime2 NOT NULL,
                    [UpdatedOn] datetime2 NOT NULL
                );
            END
            """;

        const string patchUsersGymTenantSql = """
            IF COL_LENGTH('dbo.Users', 'ProfilePhotoDataUri') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Users] ADD [ProfilePhotoDataUri] nvarchar(max) NULL;
            END

            IF COL_LENGTH('dbo.Users', 'MustChangePassword') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Users] ADD [MustChangePassword] bit NOT NULL CONSTRAINT [DF_Users_MustChangePassword] DEFAULT 0;
            END

            IF COL_LENGTH('dbo.Users', 'GymTenantId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Users] ADD [GymTenantId] uniqueidentifier NULL;
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_Users_GymTenantId'
                AND object_id = OBJECT_ID('dbo.Users')
            )
            BEGIN
                CREATE INDEX [IX_Users_GymTenantId] ON [dbo].[Users]([GymTenantId]);
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = 'FK_Users_GymTenants_GymTenantId'
            )
            BEGIN
                ALTER TABLE [dbo].[Users]
                ADD CONSTRAINT [FK_Users_GymTenants_GymTenantId]
                FOREIGN KEY ([GymTenantId]) REFERENCES [dbo].[GymTenants]([Id]) ON DELETE SET NULL;
            END
            """;

        const string patchTenantScopingColumnsSql = """
            IF COL_LENGTH('dbo.Members', 'GymTenantId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Members] ADD [GymTenantId] uniqueidentifier NULL;
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_Members_GymTenantId'
                AND object_id = OBJECT_ID('dbo.Members')
            )
            BEGIN
                CREATE INDEX [IX_Members_GymTenantId] ON [dbo].[Members]([GymTenantId]);
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = 'FK_Members_GymTenants_GymTenantId'
            )
            BEGIN
                ALTER TABLE [dbo].[Members]
                ADD CONSTRAINT [FK_Members_GymTenants_GymTenantId]
                FOREIGN KEY ([GymTenantId]) REFERENCES [dbo].[GymTenants]([Id]) ON DELETE SET NULL;
            END

            IF COL_LENGTH('dbo.MembershipPlans', 'GymTenantId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[MembershipPlans] ADD [GymTenantId] uniqueidentifier NULL;
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_MembershipPlans_GymTenantId'
                AND object_id = OBJECT_ID('dbo.MembershipPlans')
            )
            BEGIN
                CREATE INDEX [IX_MembershipPlans_GymTenantId] ON [dbo].[MembershipPlans]([GymTenantId]);
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = 'FK_MembershipPlans_GymTenants_GymTenantId'
            )
            BEGIN
                ALTER TABLE [dbo].[MembershipPlans]
                ADD CONSTRAINT [FK_MembershipPlans_GymTenants_GymTenantId]
                FOREIGN KEY ([GymTenantId]) REFERENCES [dbo].[GymTenants]([Id]) ON DELETE SET NULL;
            END

            IF COL_LENGTH('dbo.GymProfiles', 'GymTenantId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GymProfiles] ADD [GymTenantId] uniqueidentifier NULL;
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_GymProfiles_GymTenantId'
                AND object_id = OBJECT_ID('dbo.GymProfiles')
            )
            BEGIN
                CREATE INDEX [IX_GymProfiles_GymTenantId] ON [dbo].[GymProfiles]([GymTenantId]);
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = 'FK_GymProfiles_GymTenants_GymTenantId'
            )
            BEGIN
                ALTER TABLE [dbo].[GymProfiles]
                ADD CONSTRAINT [FK_GymProfiles_GymTenants_GymTenantId]
                FOREIGN KEY ([GymTenantId]) REFERENCES [dbo].[GymTenants]([Id]) ON DELETE SET NULL;
            END
            """;

        await context.Database.ExecuteSqlRawAsync(dropAssignedPlanColumnSql);
        await context.Database.ExecuteSqlRawAsync(createMembershipPlansSql);
        await context.Database.ExecuteSqlRawAsync(patchMembershipPlansTableSql);
        await context.Database.ExecuteSqlRawAsync(createMemberMembershipsSql);
        await context.Database.ExecuteSqlRawAsync(patchMemberMembershipsTableSql);
        await context.Database.ExecuteSqlRawAsync(patchMembersTableSql);
        await context.Database.ExecuteSqlRawAsync(createTrainerProfilesSql);
        await context.Database.ExecuteSqlRawAsync(createTrainerSalariesSql);
        await context.Database.ExecuteSqlRawAsync(createPaymentsSql);
        await context.Database.ExecuteSqlRawAsync(patchPaymentsTableSql);
        await context.Database.ExecuteSqlRawAsync(createInvoiceSnapshotsSql);
        await context.Database.ExecuteSqlRawAsync(createAttendancesSql);
        await context.Database.ExecuteSqlRawAsync(createGymProfileSql);
        await context.Database.ExecuteSqlRawAsync(patchGymProfilesTableSql);
        await context.Database.ExecuteSqlRawAsync(createGymTenantsSql);
        await context.Database.ExecuteSqlRawAsync(patchGymTenantsSubscriptionSql);
        await context.Database.ExecuteSqlRawAsync(unlinkLifetimeSubscriptionSql);
        await context.Database.ExecuteSqlRawAsync(createGymSubscriptionPaymentsSql);
        await context.Database.ExecuteSqlRawAsync(createSubscriptionPlansSql);
        await context.Database.ExecuteSqlRawAsync(seedSubscriptionPlansSql);
        await context.Database.ExecuteSqlRawAsync(createSuperAdminInvoiceSettingsSql);
        await context.Database.ExecuteSqlRawAsync(patchUsersGymTenantSql);
        await context.Database.ExecuteSqlRawAsync(patchTenantScopingColumnsSql);
    }
}

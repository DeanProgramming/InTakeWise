using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTakeWise.Data.Migrations
{
    public partial class StrengthenValidationAndIntegrity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            RemoveOrphanedUserData(migrationBuilder);
            RepairValuesBeforeConstraints(migrationBuilder);
            TruncateLegacyText(migrationBuilder);

            migrationBuilder.AddColumn<DateTime>(
                name: "LogDateLocal",
                table: "MealLogs",
                type: "date",
                nullable: true);

            BackfillLegacyValues(migrationBuilder);
            RemoveDuplicateSlots(migrationBuilder);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LogDateLocal",
                table: "MealLogs",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            ApplyLengthLimits(migrationBuilder);

            migrationBuilder.CreateIndex(
                name: "IX_MealLogs_UserId_LogDateLocal_TimeEat",
                table: "MealLogs",
                columns: new[]
                {
                    "UserId",
                    "LogDateLocal",
                    "TimeEat"
                },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_ShoppingMealDays_ShoppingListId",
                table: "ShoppingMealDays");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingMealDays_ShoppingListId_MealDateLocal",
                table: "ShoppingMealDays",
                columns: new[]
                {
                    "ShoppingListId",
                    "MealDateLocal"
                },
                unique: true);

            AddCheckConstraints(migrationBuilder);
            AddIdentityForeignKeys(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIdentityForeignKeys(migrationBuilder);
            DropCheckConstraints(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_MealLogs_UserId_LogDateLocal_TimeEat",
                table: "MealLogs");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingMealDays_ShoppingListId_MealDateLocal",
                table: "ShoppingMealDays");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingMealDays_ShoppingListId",
                table: "ShoppingMealDays",
                column: "ShoppingListId");

            migrationBuilder.DropColumn(
                name: "LogDateLocal",
                table: "MealLogs");

            RemoveLengthLimits(migrationBuilder);
        }

        private static void RemoveOrphanedUserData(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [UsersInformation]
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [AspNetUsers]
                    WHERE [AspNetUsers].[Id] = [UsersInformation].[UserId]
                );

                DELETE FROM [PantryItems]
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [AspNetUsers]
                    WHERE [AspNetUsers].[Id] = [PantryItems].[UserId]
                );

                DELETE FROM [MealLogs]
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [AspNetUsers]
                    WHERE [AspNetUsers].[Id] = [MealLogs].[UserId]
                );

                DELETE FROM [WorkoutLogs]
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [AspNetUsers]
                    WHERE [AspNetUsers].[Id] = [WorkoutLogs].[UserId]
                );

                DELETE FROM [ShoppingLists]
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [AspNetUsers]
                    WHERE [AspNetUsers].[Id] = [ShoppingLists].[UserId]
                );
                """);
        }

        private static void RepairValuesBeforeConstraints(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [UsersInformation]
                SET
                    [Age] =
                        CASE
                            WHEN [Age] < 13 THEN 13
                            WHEN [Age] > 120 THEN 120
                            ELSE [Age]
                        END,
                    [HeightInCM] =
                        CASE
                            WHEN [HeightInCM] < 120 THEN 120
                            WHEN [HeightInCM] > 250 THEN 250
                            ELSE [HeightInCM]
                        END,
                    [WeightInKg] =
                        CASE
                            WHEN [WeightInKg] < 30 THEN 30
                            WHEN [WeightInKg] > 350 THEN 350
                            ELSE [WeightInKg]
                        END,
                    [Gender] =
                        CASE WHEN [Gender] BETWEEN 0 AND 3
                            THEN [Gender] ELSE 3 END,
                    [EveryDayFitnessLevel] =
                        CASE WHEN [EveryDayFitnessLevel] BETWEEN 0 AND 2
                            THEN [EveryDayFitnessLevel] ELSE 1 END,
                    [ChosenGymDays] =
                        CASE WHEN [ChosenGymDays] BETWEEN 0 AND 127
                            THEN [ChosenGymDays] ELSE 0 END,
                    [ChosenFitnessGoal] =
                        CASE WHEN [ChosenFitnessGoal] BETWEEN 0 AND 4
                            THEN [ChosenFitnessGoal] ELSE 2 END,
                    [CaloriesTargetGymDay] =
                        CASE WHEN [CaloriesTargetGymDay] < 0
                            THEN 0 ELSE [CaloriesTargetGymDay] END,
                    [ProteinTargetGymDay] =
                        CASE WHEN [ProteinTargetGymDay] < 0
                            THEN 0 ELSE [ProteinTargetGymDay] END,
                    [CarbsTargetGymDay] =
                        CASE WHEN [CarbsTargetGymDay] < 0
                            THEN 0 ELSE [CarbsTargetGymDay] END,
                    [FatTargetGymDay] =
                        CASE WHEN [FatTargetGymDay] < 0
                            THEN 0 ELSE [FatTargetGymDay] END,
                    [FiberTargetGymDay] =
                        CASE WHEN [FiberTargetGymDay] < 0
                            THEN 0 ELSE [FiberTargetGymDay] END,
                    [CaloriesTargetNonGymDay] =
                        CASE WHEN [CaloriesTargetNonGymDay] < 0
                            THEN 0 ELSE [CaloriesTargetNonGymDay] END,
                    [ProteinTargetNonGymDay] =
                        CASE WHEN [ProteinTargetNonGymDay] < 0
                            THEN 0 ELSE [ProteinTargetNonGymDay] END,
                    [CarbsTargetNonGymDay] =
                        CASE WHEN [CarbsTargetNonGymDay] < 0
                            THEN 0 ELSE [CarbsTargetNonGymDay] END,
                    [FatTargetNonGymDay] =
                        CASE WHEN [FatTargetNonGymDay] < 0
                            THEN 0 ELSE [FatTargetNonGymDay] END,
                    [FiberTargetNonGymDay] =
                        CASE WHEN [FiberTargetNonGymDay] < 0
                            THEN 0 ELSE [FiberTargetNonGymDay] END;

                UPDATE [FoodItems]
                SET
                    [CaloriesPer100g] =
                        CASE WHEN [CaloriesPer100g] < 0
                            THEN 0 ELSE [CaloriesPer100g] END,
                    [ProteinPer100g] =
                        CASE WHEN [ProteinPer100g] < 0
                            THEN 0 ELSE [ProteinPer100g] END,
                    [CarbsPer100g] =
                        CASE WHEN [CarbsPer100g] < 0
                            THEN 0 ELSE [CarbsPer100g] END,
                    [FatPer100g] =
                        CASE WHEN [FatPer100g] < 0
                            THEN 0 ELSE [FatPer100g] END,
                    [FiberPer100g] =
                        CASE WHEN [FiberPer100g] < 0
                            THEN 0 ELSE [FiberPer100g] END;

                UPDATE [PantryItems]
                SET [Quantity] =
                    CASE WHEN [Quantity] < 0 THEN 0 ELSE [Quantity] END;

                UPDATE [MealLogs]
                SET
                    [TimeEat] =
                        CASE WHEN [TimeEat] BETWEEN 0 AND 3
                            THEN [TimeEat] ELSE 0 END,
                    [Calories] =
                        CASE WHEN [Calories] < 0 THEN 0 ELSE [Calories] END,
                    [Protein] =
                        CASE WHEN [Protein] < 0 THEN 0 ELSE [Protein] END,
                    [Carbs] =
                        CASE WHEN [Carbs] < 0 THEN 0 ELSE [Carbs] END,
                    [Fat] =
                        CASE WHEN [Fat] < 0 THEN 0 ELSE [Fat] END,
                    [Fiber] =
                        CASE WHEN [Fiber] < 0 THEN 0 ELSE [Fiber] END;

                UPDATE [WorkoutLogs]
                SET
                    [DurationMinutes] =
                        CASE WHEN [DurationMinutes] < 0
                            THEN 0 ELSE [DurationMinutes] END,
                    [CaloriesBurned] =
                        CASE WHEN [CaloriesBurned] < 0
                            THEN 0 ELSE [CaloriesBurned] END;

                UPDATE [ShoppingListItems]
                SET [Quantity] =
                    CASE WHEN [Quantity] <= 0 THEN 0.01 ELSE [Quantity] END;

                UPDATE [ShoppingMealDays]
                SET
                    [Calories] =
                        CASE WHEN [Calories] < 0 THEN 0 ELSE [Calories] END,
                    [ProteinGrams] =
                        CASE WHEN [ProteinGrams] < 0
                            THEN 0 ELSE [ProteinGrams] END,
                    [CarbsGrams] =
                        CASE WHEN [CarbsGrams] < 0
                            THEN 0 ELSE [CarbsGrams] END,
                    [FatGrams] =
                        CASE WHEN [FatGrams] < 0
                            THEN 0 ELSE [FatGrams] END;
                """);
        }

        private static void TruncateLegacyText(
            MigrationBuilder migrationBuilder)
        {
            if (IsSqlServer(migrationBuilder))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE [PantryItems]
                    SET [Unit] = LEFT([Unit], 30);

                    UPDATE [MealLogs]
                    SET [RawInput] = LEFT([RawInput], 2000)
                    WHERE [RawInput] IS NOT NULL;

                    UPDATE [WorkoutLogs]
                    SET
                        [RawInput] =
                            CASE WHEN [RawInput] IS NULL
                                THEN NULL ELSE LEFT([RawInput], 2000) END,
                        [ActivityType] =
                            CASE WHEN [ActivityType] IS NULL
                                THEN NULL ELSE LEFT([ActivityType], 100) END,
                        [Intensity] =
                            CASE WHEN [Intensity] IS NULL
                                THEN NULL ELSE LEFT([Intensity], 50) END;

                    UPDATE [ShoppingListItems]
                    SET
                        [Name] = LEFT([Name], 120),
                        [Unit] = LEFT([Unit], 30);

                    UPDATE [ShoppingMealDays]
                    SET [Title] = LEFT([Title], 1500);
                    """);
            }
            else if (IsSqlite(migrationBuilder))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "PantryItems"
                    SET "Unit" = substr("Unit", 1, 30);

                    UPDATE "MealLogs"
                    SET "RawInput" = substr("RawInput", 1, 2000)
                    WHERE "RawInput" IS NOT NULL;

                    UPDATE "WorkoutLogs"
                    SET
                        "RawInput" =
                            CASE WHEN "RawInput" IS NULL
                                THEN NULL ELSE substr("RawInput", 1, 2000) END,
                        "ActivityType" =
                            CASE WHEN "ActivityType" IS NULL
                                THEN NULL ELSE substr("ActivityType", 1, 100) END,
                        "Intensity" =
                            CASE WHEN "Intensity" IS NULL
                                THEN NULL ELSE substr("Intensity", 1, 50) END;

                    UPDATE "ShoppingListItems"
                    SET
                        "Name" = substr("Name", 1, 120),
                        "Unit" = substr("Unit", 1, 30);

                    UPDATE "ShoppingMealDays"
                    SET "Title" = substr("Title", 1, 1500);
                    """);
            }
            else
            {
                ThrowUnsupportedProvider(migrationBuilder);
            }
        }

        private static void BackfillLegacyValues(
            MigrationBuilder migrationBuilder)
        {
            if (IsSqlServer(migrationBuilder))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE currentFood
                    SET [NormalizedName] =
                        CASE
                            WHEN EXISTS
                            (
                                SELECT 1
                                FROM [FoodItems] AS otherFood
                                WHERE otherFood.[Id] <> currentFood.[Id]
                                  AND otherFood.[NormalizedName] =
                                      LOWER(LTRIM(RTRIM(currentFood.[Name])))
                            )
                            THEN LEFT(
                                    LOWER(LTRIM(RTRIM(currentFood.[Name]))),
                                    88)
                                 + N'~'
                                 + CONVERT(nvarchar(11), currentFood.[Id])
                            WHEN LTRIM(RTRIM(currentFood.[Name])) = N''
                            THEN N'food-'
                                 + CONVERT(nvarchar(20), currentFood.[Id])
                            ELSE LOWER(LTRIM(RTRIM(currentFood.[Name])))
                        END
                    FROM [FoodItems] AS currentFood
                    WHERE currentFood.[NormalizedName] = N'';

                    UPDATE [MealLogs]
                    SET [LogDateLocal] = CONVERT(
                        date,
                        [Timestamp] AT TIME ZONE 'UTC'
                            AT TIME ZONE 'GMT Standard Time');

                    UPDATE [ShoppingLists]
                    SET [WeekStartLocalDate] = CONVERT(date, [CreatedAt])
                    WHERE [WeekStartLocalDate] < CONVERT(date, '19000101');

                    WITH RankedPlanDays AS
                    (
                        SELECT
                            meal.[Id],
                            DATEADD(
                                day,
                                ROW_NUMBER() OVER
                                (
                                    PARTITION BY meal.[ShoppingListId]
                                    ORDER BY meal.[Id]
                                ) - 1,
                                list.[WeekStartLocalDate]) AS [BackfilledDate]
                        FROM [ShoppingMealDays] AS meal
                        INNER JOIN [ShoppingLists] AS list
                            ON list.[Id] = meal.[ShoppingListId]
                        WHERE meal.[MealDateLocal] < CONVERT(date, '19000101')
                    )
                    UPDATE meal
                    SET [MealDateLocal] = ranked.[BackfilledDate]
                    FROM [ShoppingMealDays] AS meal
                    INNER JOIN RankedPlanDays AS ranked
                        ON ranked.[Id] = meal.[Id];
                    """);
            }
            else if (IsSqlite(migrationBuilder))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "FoodItems"
                    SET "NormalizedName" =
                        CASE
                            WHEN EXISTS
                            (
                                SELECT 1
                                FROM "FoodItems" AS otherFood
                                WHERE otherFood."Id" <> "FoodItems"."Id"
                                  AND otherFood."NormalizedName" =
                                      lower(trim("FoodItems"."Name"))
                            )
                            THEN substr(
                                    lower(trim("FoodItems"."Name")),
                                    1,
                                    88)
                                 || '~'
                                 || "FoodItems"."Id"
                            WHEN trim("FoodItems"."Name") = ''
                            THEN 'food-' || "FoodItems"."Id"
                            ELSE lower(trim("FoodItems"."Name"))
                        END
                    WHERE "NormalizedName" = '';

                    UPDATE "MealLogs"
                    SET "LogDateLocal" = date("Timestamp");

                    UPDATE "ShoppingLists"
                    SET "WeekStartLocalDate" = date("CreatedAt")
                    WHERE "WeekStartLocalDate" < '1900-01-01';

                    WITH RankedPlanDays AS
                    (
                        SELECT
                            meal."Id",
                            date(
                                list."WeekStartLocalDate",
                                printf(
                                    '+%d day',
                                    ROW_NUMBER() OVER
                                    (
                                        PARTITION BY meal."ShoppingListId"
                                        ORDER BY meal."Id"
                                    ) - 1)) AS "BackfilledDate"
                        FROM "ShoppingMealDays" AS meal
                        INNER JOIN "ShoppingLists" AS list
                            ON list."Id" = meal."ShoppingListId"
                        WHERE meal."MealDateLocal" < '1900-01-01'
                    )
                    UPDATE "ShoppingMealDays"
                    SET "MealDateLocal" =
                    (
                        SELECT ranked."BackfilledDate"
                        FROM RankedPlanDays AS ranked
                        WHERE ranked."Id" = "ShoppingMealDays"."Id"
                    )
                    WHERE "Id" IN
                    (
                        SELECT "Id"
                        FROM RankedPlanDays
                    );
                    """);
            }
            else
            {
                ThrowUnsupportedProvider(migrationBuilder);
            }
        }

        private static void RemoveDuplicateSlots(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [MealLogs]
                WHERE [Id] IN
                (
                    SELECT [Id]
                    FROM
                    (
                        SELECT
                            [Id],
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY
                                    [UserId],
                                    [LogDateLocal],
                                    [TimeEat]
                                ORDER BY [Timestamp] DESC, [Id] DESC
                            ) AS [DuplicateRank]
                        FROM [MealLogs]
                    ) AS rankedMeals
                    WHERE rankedMeals.[DuplicateRank] > 1
                );

                DELETE FROM [ShoppingMealDays]
                WHERE [Id] IN
                (
                    SELECT [Id]
                    FROM
                    (
                        SELECT
                            [Id],
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY
                                    [ShoppingListId],
                                    [MealDateLocal]
                                ORDER BY [Id] DESC
                            ) AS [DuplicateRank]
                        FROM [ShoppingMealDays]
                    ) AS rankedPlanDays
                    WHERE rankedPlanDays.[DuplicateRank] > 1
                );
                """);
        }

        private static void ApplyLengthLimits(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "PantryItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RawInput",
                table: "MealLogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RawInput",
                table: "WorkoutLogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "WorkoutLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Intensity",
                table: "WorkoutLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ShoppingListItems",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "ShoppingListItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ShoppingMealDays",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);
        }

        private static void RemoveLengthLimits(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "PantryItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "RawInput",
                table: "MealLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RawInput",
                table: "WorkoutLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "WorkoutLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Intensity",
                table: "WorkoutLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ShoppingListItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "ShoppingListItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ShoppingMealDays",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1500)",
                oldMaxLength: 1500);
        }

        private static void AddCheckConstraints(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_UsersInformation_ProfileRanges",
                table: "UsersInformation",
                sql: "[Age] BETWEEN 13 AND 120 " +
                     "AND [HeightInCM] BETWEEN 120 AND 250 " +
                     "AND [WeightInKg] BETWEEN 30 AND 350");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsersInformation_Enums",
                table: "UsersInformation",
                sql: "[Gender] BETWEEN 0 AND 3 " +
                     "AND [EveryDayFitnessLevel] BETWEEN 0 AND 2 " +
                     "AND [ChosenGymDays] BETWEEN 0 AND 127 " +
                     "AND [ChosenFitnessGoal] BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsersInformation_Targets_NonNegative",
                table: "UsersInformation",
                sql: "[CaloriesTargetGymDay] >= 0 " +
                     "AND [ProteinTargetGymDay] >= 0 " +
                     "AND [CarbsTargetGymDay] >= 0 " +
                     "AND [FatTargetGymDay] >= 0 " +
                     "AND [FiberTargetGymDay] >= 0 " +
                     "AND [CaloriesTargetNonGymDay] >= 0 " +
                     "AND [ProteinTargetNonGymDay] >= 0 " +
                     "AND [CarbsTargetNonGymDay] >= 0 " +
                     "AND [FatTargetNonGymDay] >= 0 " +
                     "AND [FiberTargetNonGymDay] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FoodItems_Nutrition_NonNegative",
                table: "FoodItems",
                sql: "([CaloriesPer100g] IS NULL OR [CaloriesPer100g] >= 0) " +
                     "AND ([ProteinPer100g] IS NULL OR [ProteinPer100g] >= 0) " +
                     "AND ([CarbsPer100g] IS NULL OR [CarbsPer100g] >= 0) " +
                     "AND ([FatPer100g] IS NULL OR [FatPer100g] >= 0) " +
                     "AND ([FiberPer100g] IS NULL OR [FiberPer100g] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PantryItems_Quantity_NonNegative",
                table: "PantryItems",
                sql: "[Quantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MealLogs_TimeEat",
                table: "MealLogs",
                sql: "[TimeEat] BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MealLogs_Nutrition_NonNegative",
                table: "MealLogs",
                sql: "([Calories] IS NULL OR [Calories] >= 0) " +
                     "AND ([Protein] IS NULL OR [Protein] >= 0) " +
                     "AND ([Carbs] IS NULL OR [Carbs] >= 0) " +
                     "AND ([Fat] IS NULL OR [Fat] >= 0) " +
                     "AND ([Fiber] IS NULL OR [Fiber] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkoutLogs_Values_NonNegative",
                table: "WorkoutLogs",
                sql: "([DurationMinutes] IS NULL OR [DurationMinutes] >= 0) " +
                     "AND ([CaloriesBurned] IS NULL OR [CaloriesBurned] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShoppingListItems_Quantity_Positive",
                table: "ShoppingListItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShoppingMealDays_Nutrition_NonNegative",
                table: "ShoppingMealDays",
                sql: "[Calories] >= 0 " +
                     "AND [ProteinGrams] >= 0 " +
                     "AND [CarbsGrams] >= 0 " +
                     "AND [FatGrams] >= 0");
        }

        private static void DropCheckConstraints(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UsersInformation_ProfileRanges",
                table: "UsersInformation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsersInformation_Enums",
                table: "UsersInformation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsersInformation_Targets_NonNegative",
                table: "UsersInformation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FoodItems_Nutrition_NonNegative",
                table: "FoodItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PantryItems_Quantity_NonNegative",
                table: "PantryItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MealLogs_TimeEat",
                table: "MealLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MealLogs_Nutrition_NonNegative",
                table: "MealLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkoutLogs_Values_NonNegative",
                table: "WorkoutLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ShoppingListItems_Quantity_Positive",
                table: "ShoppingListItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ShoppingMealDays_Nutrition_NonNegative",
                table: "ShoppingMealDays");
        }

        private static void AddIdentityForeignKeys(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_UsersInformation_AspNetUsers_UserId",
                table: "UsersInformation",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PantryItems_AspNetUsers_UserId",
                table: "PantryItems",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MealLogs_AspNetUsers_UserId",
                table: "MealLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutLogs_AspNetUsers_UserId",
                table: "WorkoutLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingLists_AspNetUsers_UserId",
                table: "ShoppingLists",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void DropIdentityForeignKeys(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsersInformation_AspNetUsers_UserId",
                table: "UsersInformation");

            migrationBuilder.DropForeignKey(
                name: "FK_PantryItems_AspNetUsers_UserId",
                table: "PantryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MealLogs_AspNetUsers_UserId",
                table: "MealLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutLogs_AspNetUsers_UserId",
                table: "WorkoutLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingLists_AspNetUsers_UserId",
                table: "ShoppingLists");
        }

        private static bool IsSqlServer(
            MigrationBuilder migrationBuilder) =>
            migrationBuilder.ActiveProvider.Contains(
                "SqlServer",
                StringComparison.OrdinalIgnoreCase);

        private static bool IsSqlite(
            MigrationBuilder migrationBuilder) =>
            migrationBuilder.ActiveProvider.Contains(
                "Sqlite",
                StringComparison.OrdinalIgnoreCase);

        private static void ThrowUnsupportedProvider(
            MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException(
                $"Data-integrity backfill is not configured for provider '{migrationBuilder.ActiveProvider}'.");
    }
}
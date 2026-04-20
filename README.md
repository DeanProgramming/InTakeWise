# InTakeWise

## Overview

**InTakeWise** is a full-stack web application built with **ASP.NET Core MVC**, designed to help users manage their nutrition, pantry, training, and weekly meal planning in one place. Users can create a profile with fitness goals and gym-day settings, log meals and workouts, manage pantry items, and generate a tailored shopping plan based on what they already have at home.

The project uses **SQL Server** for persistence, includes secure **user authentication with ASP.NET Identity**, and integrates **OpenAI-powered analysis** to estimate meal macros, workout calorie burn, and generate personalised shopping and meal suggestions.

## Project URL
[InTakeWise](https://intakewise.azurewebsites.net/)

## Features

- **Secure Authentication & User Accounts:**  
  Users can register, sign in, and manage their own nutrition, pantry, and profile data using **ASP.NET Identity**.

- **Profile Setup Wizard:**  
  Users can complete a 3-step profile flow to configure:
  - basic personal details
  - daily activity level
  - gym days
  - fitness goal
  - estimated calorie and macro targets for gym and non-gym days

- **Meal Logging with AI Analysis:**  
  Users can log meals in natural language and receive estimated:
  - calories
  - protein
  - carbs
  - fat
  - fiber

- **Workout Logging with AI Analysis:**  
  Users can log workouts in natural language and receive estimated:
  - workout type
  - duration
  - intensity
  - calories burned

- **Daily Summary Dashboard:**  
  The app tracks progress against daily macro and calorie targets, including:
  - calories eaten
  - protein / carbs / fat / fiber consumed
  - workout calories burned
  - remaining targets for the day
  - net calories

- **Pantry Management:**
  - Add pantry items manually
  - Edit or remove pantry items
  - Merge duplicate food rows by name
  - Automatically combine compatible units such as g/kg or ml/l

- **Receipt to Pantry Workflow:**  
  Includes a receipt-to-pantry flow that parses receipt-style entries into pantry items and allows users to review and save them into their pantry.

- **AI-Powered Weekly Shopping Plan:**  
  Generate a shopping list and multi-day meal plan based on:
  - current pantry contents
  - user profile
  - daily calorie and macro targets
  - gym vs non-gym days

- **Today’s Meal Suggestions:**  
  Users can view meal suggestions for the current day, including:
  - overview
  - ingredients
  - steps
  - daily macro totals

- **London Timezone-Aware Logging:**  
  The app uses a dedicated clock service to ensure “today” and daily summaries are calculated consistently using **Europe/London** time.

## System Architecture

The InTakeWise project follows a layered structure:

1. **Presentation Layer**
   - ASP.NET Core MVC Controllers
   - Razor Views
   - View Components for reusable UI sections
   - Client-side JavaScript for interactive forms and draft-saving behaviour

2. **Application Layer**
   - Controllers coordinate user flows such as logging meals, updating pantry items, and generating plans
   - Services encapsulate business logic for:
     - meal and workout logging
     - pantry merging
     - meal suggestions
     - shopping plan generation
     - time/date handling

3. **Data Layer**
   - Entity Framework Core (EF Core) for ORM and database access
   - SQL Server for persistence
   - Identity tables for authentication and account management

4. **AI / Integration Layer**
   - OpenAI integration for:
     - meal nutrition estimation
     - workout calorie estimation
     - shopping list and weekly meal plan generation

## Core Technologies

- **ASP.NET Core MVC**
- **Entity Framework Core**
- **SQL Server**
- **ASP.NET Identity**
- **OpenAI Chat API**
- **Razor Views**
- **JavaScript**
- **Azure-ready deployment setup**

## Data Model Highlights

Key entities in the project include:

- `UsersInformation`
- `FoodItem`
- `PantryItem`
- `MealLogEntry`
- `WorkoutLogEntry`
- `ShoppingList`
- `ShoppingListItem`
- `ShoppingMealDay`

These models support the app’s core flows around profile setup, pantry tracking, daily logging, and meal planning.

## Key Workflows

### 1. Profile Setup
Users complete a guided profile wizard which stores:
- age
- gender
- height
- weight
- daily activity level
- chosen gym days
- fitness goal

The app then calculates rough calorie and macro targets for gym and non-gym days.

### 2. Logging Meals and Workouts
Users can write entries in natural language. The system sends the text to an AI parser and stores the structured result in the database.

### 3. Pantry + Shopping Flow
Users can:
- manually manage pantry items
- import receipt-style entries into the pantry
- generate a shopping plan that fills gaps based on pantry contents and profile targets

### 4. Today’s Meal Plan
Once a shopping plan exists, users can view the suggested meals for the current day and follow the proposed ingredients and steps.

## Notes 
- The application uses a custom clock abstraction to avoid date/time inconsistencies.
- Pantry units are normalised and merged where possible.
- Shopping plans are tied to the authenticated user.
- The OpenAI model used for analysis and planning is configurable through app configuration.

## Running the Project Locally

1. Clone the repository
2. Configure your SQL Server connection string
3. Add your OpenAI API key
4. Run Entity Framework migrations
5. Start the application

Example configuration requirements:

- `ConnectionStrings:DefaultConnection`
- `OpenAI:ApiKey`
- optional OpenAI model settings for logging and shopping generation

## Summary

**InTakeWise** is a practical nutrition and fitness assistant built with **ASP.NET Core MVC**, combining traditional full-stack web development with AI-powered analysis. It helps users move from raw inputs like “what I ate,” “what I trained,” and “what’s in my pantry” into structured daily tracking and personalised planning.
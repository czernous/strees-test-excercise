# Stress Test Application - Exercise Instructions

## Introduction

The aim of this exercise is to produce an application that includes the functionality described in each of the sections below. Each section is related and they should be thought of as a single, integrated application rather than separate exercises. The application will be judged on the quality of the code, as well as the end-result and you should aim to make your code maintainable, flexible, modular and readable. Performance is a consideration with a faster solution being considered more optimal.

## Data Files

There are three data files supplied with this exercise:
- **Portfolios.csv** - Contains details of a number of portfolios including their country and currency
- **Loans.csv** - Contains a list of home loans contained in those portfolios
- **Ratings.csv** - Lists the potential credit ratings that a portfolio may have and the probability that a loan may default

---

## Section 1

Using C# and any user-interface technology of your choice (web is preferred but not mandatory), implement a solution that achieves the following objectives.

### A. User Interface

Create a user interface that allows a user to specify a percentage change in house prices for each country in the example list below:

| Country | Percentage Change |
|---------|-------------------|
| GB      | -5.12            |
| US      | -4.34            |
| FR      | -3.87            |
| DE      | -1.23            |
| SG      | -5.5             |
| GR      | -5.68            |

### B. Backend Service

Create a backend service that accepts the percentage changes, reads in the csv files and aggregates the following calculations grouped by the portfolio:

- Sum the total **Outstanding Loan Amount** and **Collateral Value**
- Calculate and sum the **Scenario Collateral Value** and **Expected Loss** as indicated below
- You will need to look up the **Probability of Default (PD)** by the credit rating in Ratings.csv

#### Formulas:

```
Scenario Collateral Value = Collateral Value * (1 + Percentage Change / 100)
Recovery Rate (RR) = Scenario Collateral Value / Original Loan Amount
Loss Given Default (LGD) = 1 – Recovery Rate
Expected Loss (EL) = PD * LGD * Outstanding Amount
```

**Note:** You can assume the csv files are saved locally on the server. These calculations should be fully tested.

---

## Section 2 - Databases

This section of the exercise is for you to demonstrate the use of SQL and some form of persistent, relational data store. Again, you are free to choose here but if you work on a server-based database please supply the SQL code to create the database. If you use a file-based database please supply the file with your submission.

Extend the code you wrote for section 1 so that for each run of the application:

### A. Save Run Metadata

Saves the date and time it took place, the percentage inputs used for each country and statistics/metadata of your choosing about the run i.e. time taken, logging etc.

### B. Save Aggregated Results

Saves the aggregated results of the run created in Section 1B.

---

## Section 3

### A. Display Run History

Extend your user interface to display details of all runs along with the ability to retrieve the aggregated results generated during Section 1B.

---

## Evaluation Criteria

- **Code Quality**: Maintainable, flexible, modular, and readable code
- **Performance**: Faster solutions are considered more optimal
- **Completeness**: All sections implemented and integrated
- **Testing**: Calculations should be fully tested

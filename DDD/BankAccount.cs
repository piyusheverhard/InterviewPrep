
/*
  The Scenario:
  You are building the core domain for a FinTech app. We need to model a BankAccount and the concept 
  of Money.
  
  Here are the strict business rules (Invariants):
  
  1. Money (Value Object): Must have an Amount (decimal) and a Currency (string, e.g., "USD"). It    
  must be immutable. You cannot have negative money.
  2. BankAccount (Entity): Has an Id and a Balance (which must be a Money object).
  3. You can Deposit money into the account.
  4. You can Withdraw money from the account, but the balance cannot go below zero.
  5. Currency Safety: You cannot deposit or withdraw EUR into a USD account. It must throw a domain  
  exception.
  
  Your Task:
  Write the pure C# domain classes for BankAccount (Entity) and Money (Value Object).
  Show me how you use private set and constructors to enforce these business rules so that no junior 
  developer can ever illegally set the balance to negative or mix currencies. You do not need to     
  write the database or service layer—just the pure domain models.

  */

namespace BankAccount;

public enum Currency
{
    USD,
    INR,
    EUR,
}

public record Money
{
    public decimal Amount
    {
        get; init
        {
            if (value < 0)
            {
                throw new ArgumentException("Amount can't be negative.");
            }
            field = value;
        }
    }
    public readonly Currency Currency;

    public Money()
    {
        Amount = 0;
        Currency = Currency.USD;
    }

    public Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money money)
    {
        if (money.Currency != this.Currency)
        {
            throw new ArgumentException("Currency of money to be added must be same.");
        }
        return new Money(this.Amount + money.Amount, this.Currency);
    }

    public Money Deduct(Money money)
    {
        if (money.Currency != this.Currency)
        {
            throw new ArgumentException("Currency of money to be deducted must be same.");
        }
        return new Money(this.Amount - money.Amount, this.Currency);
    }
}

public class BankAccount
{
    public string Id { get; init; }
    public Money Balance { get; set; }

    public BankAccount(string id, Currency currency = Currency.USD)
    {
        Id = id;
        Balance = new(0, currency);
    }

    public BankAccount(string id, decimal initialAmount, Currency currency = Currency.USD)
    {
        Id = id;
        Balance = new(initialAmount, currency);
    }

    public void Deposit(Money amount) => Balance = Balance.Add(amount);

    public void Witdhdraw(Money amount) => Balance = Balance.Deduct(amount);
}

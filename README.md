# Payment playground project

Takes your card, asks the bank nicely, remembers what it said.
The bank is fake. Your money is safe. 🙂

## What you need

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker: [Docker Desktop](https://www.docker.com/products/docker-desktop/) on Mac and Windows, [Docker Engine](https://docs.docker.com/engine/install/) on Linux

## Run it

Same on Linux, Mac and Windows:

```sh
docker compose up -d                          # wake up the fake bank
dotnet run --project src/PaymentGateway.Apis  # start the gateway on :5260
```

## Pay for something

**Linux / Mac**

```sh
curl -i http://localhost:5260/api/payments \
  -H "Content-Type: application/json" \
  -d '{"card_number":"2222405343248877","expiry_month":4,"expiry_year":2030,"currency":"GBP","amount":100,"cvv":"123"}'
```

Got an `id` back? Fetch it again:

```sh
curl http://localhost:5260/api/payments/<id>
```

**Windows (PowerShell)**

```powershell
$body = @{
    card_number  = "2222405343248877"
    expiry_month = 4
    expiry_year  = 2030
    currency     = "GBP"
    amount       = 100
    cvv          = "123"
} | ConvertTo-Json

$payment = Invoke-RestMethod http://localhost:5260/api/payments -Method Post -ContentType "application/json" -Body $body
$payment
```

Fetch it again:

```powershell
Invoke-RestMethod "http://localhost:5260/api/payments/$($payment.id)"
```

## Magic cards 🎩

The fake bank only looks at the **last digit**:

| Ends in | You get |
|---|---|
| 1 3 5 7 9 | ✅ Authorized |
| 2 4 6 8 | ❌ Declined |
| 0 | 💥 Bank is down (502) |

Send garbage and you get a `400 Rejected` with the list of everything you got wrong.

## Test it

```sh
dotnet test
```

No Docker needed. The tests bring their own fake bank.

## Stop it

`Ctrl+C`, then:

```sh
docker compose down
```

## Something broke?

- **Every payment is a 502?** The bank is asleep. Run `docker compose up -d`. On Mac and Windows, make sure Docker Desktop is open.
- **Docker says permission denied? (Linux)** `sudo usermod -aG docker $USER`, then log out and back in.
- **PowerShell shows a red error for a 400 or 502?** That's `Invoke-RestMethod` being dramatic. The message is the gateway's answer.

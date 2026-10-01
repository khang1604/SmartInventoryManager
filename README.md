# SMART INVENTORY MANAGER

á»¨ng dá»¥ng desktop quáº£n lÃ½ kho dÃ nh cho doanh nghiá»‡p/cá»­a hÃ ng cÃ³ má»™t hoáº·c nhiá»u kho. Dá»± Ã¡n phá»¥c vá»¥ mÃ´n Láº­p trÃ¬nh Windows Forms, sá»­ dá»¥ng C#, .NET vÃ  SQL Server.

> TÃ i liá»‡u nÃ y mÃ´ táº£ cÃ¡ch tham gia vÃ  phÃ¡t triá»ƒn dá»± Ã¡n. Danh sÃ¡ch chá»©c nÄƒng lÃ  pháº¡m vi dá»± kiáº¿n, khÃ´ng pháº£i xÃ¡c nháº­n táº¥t cáº£ Ä‘Ã£ hoÃ n thiá»‡n. File hoáº·c mÃ n hÃ¬nh khung chÆ°a cÃ³ nghiá»‡p vá»¥ Ä‘Æ°á»£c xem lÃ  chÆ°a triá»ƒn khai.

## 1. CÃ´ng nghá»‡ sá»­ dá»¥ng

| ThÃ nh pháº§n | CÃ´ng nghá»‡ / quy Ä‘á»‹nh |
|---|---|
| Há»‡ Ä‘iá»u hÃ nh | Windows phÃ¹ há»£p yÃªu cáº§u há»— trá»£ cá»§a .NET vÃ  Visual Studio; Æ°u tiÃªn Windows 11 |
| NgÃ´n ngá»¯ | C# |
| Ná»n táº£ng máº·c Ä‘á»‹nh | **.NET 10**, khÃ´ng pháº£i â€œ.NET Framework 10â€ |
| Giao diá»‡n | Windows Forms |
| MÃ´i trÆ°á»ng phÃ¡t triá»ƒn máº·c Ä‘á»‹nh | Visual Studio 2026, workload `.NET desktop development` |
| Database | Microsoft SQL Server; instance local phÃ¹ há»£p cho phÃ¡t triá»ƒn |
| ORM | Entity Framework Core 10.x náº¿u solution dÃ¹ng .NET 10 |
| Truy váº¥n | LINQ |
| Truy cáº­p SQL bá»• sung | ADO.NET / Microsoft.Data.SqlClient |
| Quáº£n lÃ½ thÆ° viá»‡n | NuGet, thÃ´ng qua Visual Studio hoáº·c dotnet CLI |
| Quáº£n lÃ½ mÃ£ nguá»“n | Git / GitHub |

PhiÃªn báº£n tháº­t Ä‘Æ°á»£c xÃ¡c Ä‘á»‹nh bá»Ÿi `.csproj`, `global.json` vÃ  `Directory.Packages.props` náº¿u cÃ³. Náº¿u repository Ä‘Ã£ dÃ¹ng .NET 8/EF Core 8.x, giá»¯ cáº¥u hÃ¬nh Ä‘Ã³ vÃ  dÃ¹ng Visual Studio tÆ°Æ¡ng thÃ­ch. KhÃ´ng tá»± nÃ¢ng phiÃªn báº£n hoáº·c trá»™n EF6 vá»›i EF Core.

- EF: CRUD thÃ´ng thÆ°á»ng.
- LINQ: truy váº¥n, lá»c, tá»•ng há»£p vÃ  phÃ¢n tÃ­ch.
- ADO.NET: Stored Procedure, truy váº¥n bÃ¡o cÃ¡o hoáº·c SQL phá»©c táº¡p khi cáº§n.
- KhÃ´ng sá»­ dá»¥ng Next.js, Mantine, MongoDB, npm/yarn hoáº·c cÃ´ng nghá»‡ web trong dá»± Ã¡n nÃ y.
- ThÃªm thÆ° viá»‡n má»›i, Ä‘á»•i cÃ´ng nghá»‡ hoáº·c nÃ¢ng phiÃªn báº£n lá»›n pháº£i Ä‘Æ°á»£c nhÃ³m thá»‘ng nháº¥t trÆ°á»›c.

## 2. Pháº¡m vi chá»©c nÄƒng

- ÄÄƒng nháº­p vÃ  phÃ¢n quyá»n Admin / Quáº£n lÃ½ kho / NhÃ¢n viÃªn kho.
- Quáº£n lÃ½ sáº£n pháº©m, danh má»¥c, nhÃ  cung cáº¥p, kho vÃ  vá»‹ trÃ­ kho.
- Nháº­p kho, xuáº¥t kho, kiá»ƒm kÃª, Ä‘iá»u chá»‰nh vÃ  chuyá»ƒn kho.
- Theo dÃµi tá»“n theo sáº£n pháº©m vÃ  tá»«ng kho; lÆ°u lá»‹ch sá»­ biáº¿n Ä‘á»™ng.
- Cáº£nh bÃ¡o háº¿t hÃ ng, sáº¯p háº¿t hÃ ng vÃ  tá»“n vÆ°á»£t má»©c.
- Dashboard, thá»‘ng kÃª, biá»ƒu Ä‘á»“, bÃ¡o cÃ¡o vÃ  phÃ¢n tÃ­ch theo tiÃªu chÃ­ Ä‘Ã£ thá»‘ng nháº¥t.

## 3. PhÃ¢n cÃ´ng vÃ  phá»‘i há»£p

| ThÃ nh viÃªn | Phá»¥ trÃ¡ch chÃ­nh |
|---|---|
| **Báº£o Anh** | Database, ERD, Entity, DbContext, kiáº¿n trÃºc, SQL scripts; Repository/UnitOfWork khi cÃ³ giÃ¡ trá»‹ rÃµ rÃ ng |
| **Khang** | Business Services, nháº­p/xuáº¥t, tá»“n kho, Ä‘iá»u chá»‰nh/chuyá»ƒn kho, backend cáº£nh bÃ¡o, kiá»ƒm tra vÃ  kiá»ƒm thá»­ nghiá»‡p vá»¥ |
| **KhiÃªm** | Windows Forms UI, Dashboard, Analytics, biá»ƒu Ä‘á»“, bÃ¡o cÃ¡o vÃ  thÃ´ng bÃ¡o trÃªn giao diá»‡n |

- Má»—i ngÆ°á»i sá»­a pháº§n Ä‘Æ°á»£c giao. ÄÆ°á»£c Ä‘á»c code táº§ng khÃ¡c Ä‘á»ƒ hiá»ƒu cÃ¡ch káº¿t ná»‘i.
- Náº¿u cáº§n sá»­a pháº§n ngÆ°á»i khÃ¡c, thá»‘ng nháº¥t thÃ nh pháº§n, má»¥c Ä‘Ã­ch vÃ  quyá»n sá»­a trÆ°á»›c.
- Database, DTO, Interface, Common, Ä‘Äƒng nháº­p vÃ  cáº¥u hÃ¬nh solution lÃ  cÃ¡c Ä‘iá»ƒm cáº§n phá»‘i há»£p; khÃ´ng tá»± Ä‘á»•i há»£p Ä‘á»“ng chung.
- Khi thay Ä‘á»•i liÃªn táº§ng, nÃªu rÃµ ai lÃ m pháº§n dá»¯ liá»‡u, ai lÃ m nghiá»‡p vá»¥, ai ná»‘i giao diá»‡n.
- KhÃ´ng táº¡o Service/Repository thay tháº¿ hoáº·c SQL trá»±c tiáº¿p trÃªn Form Ä‘á»ƒ Ä‘i vÃ²ng qua pháº§n chÆ°a hoÃ n thiá»‡n.

## 4. Cáº¥u trÃºc vÃ  kiáº¿n trÃºc

| ÄÆ°á»ng dáº«n | Ná»™i dung |
|---|---|
| `SmartInventoryManager.sln` hoáº·c `.slnx` | Solution cá»§a dá»± Ã¡n |
| `SmartInventoryManager.UI/` | Forms, Controls, Components, Themes, Resources, Program.cs |
| `SmartInventoryManager.Business/` | Services, Interfaces, DTOs, Validators, Events |
| `SmartInventoryManager.Data/` | Context, Entities, Configurations, AdoNet, Queries, Repository náº¿u Ä‘Æ°á»£c dÃ¹ng |
| `SmartInventoryManager.Common/` | Enums, Helpers, Security, Session |
| `SmartInventoryManager.Tests/` | Khung kiá»ƒm thá»­; test framework chá»‰ Ä‘Æ°á»£c thÃªm khi nhÃ³m phÃª duyá»‡t |
| `Database/` | Script táº¡o vÃ  cáº­p nháº­t database |
| `Documentation/` | YÃªu cáº§u, thiáº¿t káº¿, bÃ¡o cÃ¡o vÃ  tÃ i liá»‡u tham kháº£o |
| `.github/copilot-instructions.md` | Quy táº¯c AI Agent dÃ¹ng chung, Ä‘Æ°á»£c chia sáº» qua Git |
| `.agent/RULE.md` | Báº£n quy táº¯c cÃ¡ nhÃ¢n, khÃ´ng commit |

Tham chiáº¿u máº·c Ä‘á»‹nh:

- UI â†’ Business, Common.
- Business â†’ Data, Common.
- Data â†’ Common.
- Common khÃ´ng phá»¥ thuá»™c ngÆ°á»£c cÃ¡c táº§ng khÃ¡c.

UI hiá»ƒn thá»‹ vÃ  gá»i Service. Business xá»­ lÃ½ nghiá»‡p vá»¥. Data truy xuáº¥t/lÆ°u dá»¯ liá»‡u. KhÃ´ng Ä‘áº·t SQL, truy váº¥n DbContext hoáº·c cÃ´ng thá»©c tá»“n kho trong Form. UI chá»‰ tham chiáº¿u Data táº¡i Ä‘iá»ƒm khá»Ÿi táº¡o khi kiáº¿n trÃºc Ä‘Ã£ chá»n thá»±c sá»± cáº§n.

## 5. Chuáº©n bá»‹ mÃ´i trÆ°á»ng

Má»—i thÃ nh viÃªn chuáº©n bá»‹:

1. Visual Studio tÆ°Æ¡ng thÃ­ch framework cá»§a repository; cÃ i workload `.NET desktop development` vÃ  SDK tÆ°Æ¡ng á»©ng.
2. Git.
3. SQL Server instance dÃ¹ng cho phÃ¡t triá»ƒn. CÃ³ thá»ƒ dÃ¹ng LocalDB trÃªn tá»«ng mÃ¡y náº¿u phÃ¹ há»£p cáº¥u hÃ¬nh nhÃ³m, hoáº·c SQL Server Express/Developer phÃ¹ há»£p má»¥c Ä‘Ã­ch sá»­ dá»¥ng.
4. SSMS náº¿u cáº§n quáº£n lÃ½ vÃ  cháº¡y SQL script. SSMS lÃ  cÃ´ng cá»¥ quáº£n lÃ½, khÃ´ng pháº£i SQL Server vÃ  khÃ´ng pháº£i gÃ³i NuGet.
5. GitHub Copilot trong Visual Studio náº¿u sá»­ dá»¥ng AI Agent; Ä‘Ã¢y lÃ  tÃ¹y chá»n, khÃ´ng báº¯t buá»™c Ä‘á»ƒ build/cháº¡y á»©ng dá»¥ng.

Trong Developer PowerShell kiá»ƒm tra:

```powershell
dotnet --list-sdks
git --version
```

`dotnet restore` chá»‰ táº£i thÆ° viá»‡n cá»§a dá»± Ã¡n; khÃ´ng cÃ i SQL Server, SDK hoáº·c workload Visual Studio.

## 6. Láº¥y mÃ£ nguá»“n vÃ  cháº¡y láº§n Ä‘áº§u

### 6.1. Clone repository

Láº¥y URL tháº­t táº¡i GitHub â†’ Code. Thay `<URL_REPOSITORY>` vÃ  `<THU_MUC_REPOSITORY>` trong vÃ­ dá»¥ sau báº±ng thÃ´ng tin thá»±c táº¿:

```powershell
git clone <URL_REPOSITORY>
cd <THU_MUC_REPOSITORY>
git branch -a
```

Náº¿u nhÃ³m sá»­ dá»¥ng nhÃ¡nh `develop` vÃ  nhÃ¡nh Ä‘Ã£ tá»“n táº¡i trÃªn remote:

```powershell
git switch develop
git pull --ff-only origin develop
```

Trong repository này, `develop` là nhánh chính và m?c d?nh.

### 6.2. Má»Ÿ solution

- Má»Ÿ `.sln` hoáº·c `.slnx` hiá»‡n cÃ³ trong Visual Studio.
- KhÃ´ng táº¡o project/solution má»›i sau khi clone.
- Chá»n project UI lÃ m Startup Project: chuá»™t pháº£i project UI â†’ **Set as Startup Project**.

### 6.3. Restore vÃ  build

Cháº¡y táº¡i thÆ° má»¥c chá»©a solution. Náº¿u solution náº±m trong thÆ° má»¥c con cá»§a repository, chuyá»ƒn vÃ o thÆ° má»¥c Ä‘Ã³ trÆ°á»›c:

```powershell
dotnet restore
dotnet build --no-restore
```

Náº¿u bÃ¡o thiáº¿u SDK hoáº·c package khÃ´ng tÆ°Æ¡ng thÃ­ch, kiá»ƒm tra cáº¥u hÃ¬nh repository; khÃ´ng tá»± nÃ¢ng framework Ä‘á»ƒ bá» qua lá»—i.

### 6.4. Thiáº¿t láº­p database

1. Láº¥y báº£n SQL script/schema Ä‘Æ°á»£c Báº£o Anh xÃ¡c nháº­n Ä‘Ã£ hoÃ n thiá»‡n.
2. Káº¿t ná»‘i Ä‘Ãºng SQL Server instance trÃªn mÃ¡y báº±ng SSMS hoáº·c cÃ´ng cá»¥ nhÃ³m Ä‘ang dÃ¹ng.
3. Cháº¡y script theo thá»© tá»± vÃ  hÆ°á»›ng dáº«n cá»§a báº£n schema Ä‘Ã³. CÃ¡c file Ä‘Æ°á»£c Ä‘Ã¡nh sá»‘ `01` Ä‘áº¿n `08` trong `Database/` lÃ  cáº¥u trÃºc dá»± kiáº¿n; file chá»‰ cÃ³ comment chÆ°a pháº£i script hoáº¡t Ä‘á»™ng.
4. Seed data chá»‰ cháº¡y khi Ä‘Ã£ Ä‘Æ°á»£c nhÃ³m duyá»‡t. KhÃ´ng tá»± táº¡o tÃ i khoáº£n/máº­t kháº©u chung hoáº·c cháº¡y seed nhiá»u láº§n gÃ¢y trÃ¹ng dá»¯ liá»‡u.
5. Cáº¥u hÃ¬nh connection string báº±ng cÆ¡ cháº¿ á»©ng dá»¥ng Ä‘Ã£ triá»ƒn khai. Náº¿u chÆ°a cÃ³ cÆ¡ cháº¿ Ä‘á»c cáº¥u hÃ¬nh, cáº§n hoÃ n thiá»‡n vá»›i ngÆ°á»i phá»¥ trÃ¡ch; khÃ´ng tá»± táº¡o `appsettings.json` hoáº·c máº·c Ä‘á»‹nh `App.config` sáº½ Ä‘Æ°á»£c Ä‘á»c.
6. KhÃ´ng tá»± cháº¡y `Update-Database`, migration vÃ  script táº¡o schema cÃ¹ng lÃºc. NhÃ³m chá»n má»™t nguá»“n quáº£n lÃ½ schema rÃµ rÃ ng.

VÃ­ dá»¥ connection string local, thay database/instance báº±ng tÃªn tháº­t Ä‘Ã£ thá»‘ng nháº¥t:

```text
Server=(localdb)\MSSQLLocalDB;Database=SmartInventoryDb;Integrated Security=True;
```

Hoáº·c náº¿u mÃ¡y Ä‘Ã£ cÃ³ instance SQL Server Express tÃªn `SQLEXPRESS`:

```text
Server=.\SQLEXPRESS;Database=SmartInventoryDb;Integrated Security=True;
```

ÄÃ¢y lÃ  vÃ­ dá»¥ káº¿t ná»‘i, khÃ´ng kháº³ng Ä‘á»‹nh database hoáº·c instance nÃ y Ä‘Ã£ cÃ³ trÃªn mÃ¡y. ThÃ´ng tin Ä‘Äƒng nháº­p vÃ  cáº¥u hÃ¬nh chá»©a bÃ­ máº­t pháº£i Ä‘Æ°á»£c lÆ°u theo cÆ¡ cháº¿ local cá»§a nhÃ³m, khÃ´ng commit.

### 6.5. Cháº¡y á»©ng dá»¥ng

Nháº¥n **F5** trong Visual Studio, hoáº·c náº¿u project cÃ³ Ä‘Ãºng tÃªn/Ä‘Æ°á»ng dáº«n bÃªn dÆ°á»›i:

```powershell
dotnet run --project SmartInventoryManager.UI/SmartInventoryManager.UI.csproj
```

Náº¿u project UI Ä‘ang cÃ³ tÃªn khÃ¡c, dÃ¹ng Ä‘Æ°á»ng dáº«n `.csproj` thá»±c táº¿. TÃ i khoáº£n demo do nhÃ³m cung cáº¥p sau khi pháº§n Ä‘Äƒng nháº­p vÃ  seed Ä‘Ã£ hoÃ n thiá»‡n. MÃ n hÃ¬nh khung cháº¡y Ä‘Æ°á»£c chÆ°a chá»©ng minh chá»©c nÄƒng nghiá»‡p vá»¥ Ä‘Ã£ hoÃ n thÃ nh.

## 7. Quy táº¯c nghiá»‡p vá»¥ quan trá»ng

- Tá»“n kho theo sáº£n pháº©m + kho, khÃ´ng chá»‰ theo sáº£n pháº©m.
- Business Service lÃ  nÆ¡i Ä‘iá»u phá»‘i cáº­p nháº­t tá»“n. KhÃ´ng dÃ¹ng Trigger tá»± tÄƒng/giáº£m Inventory.
- Phiáº¿u, chi tiáº¿t, tá»“n vÃ  lá»‹ch sá»­ pháº£i nháº¥t quÃ¡n trong transaction.
- Má»™t phiáº¿u chá»‰ tÃ¡c Ä‘á»™ng tá»“n má»™t láº§n; chá»‘ng gá»­i láº¡i vÃ  thao tÃ¡c Ä‘á»“ng thá»i.
- KhÃ´ng xuáº¥t vÆ°á»£t tá»“n hoáº·c cho tá»“n Ã¢m khi chÆ°a cÃ³ nghiá»‡p vá»¥ cho phÃ©p.
- Sá»± kiá»‡n cáº­p nháº­t UI/cáº£nh bÃ¡o phÃ¡t sau commit thÃ nh cÃ´ng.
- Äiá»u chá»‰nh pháº£i cÃ³ lÃ½ do, ngÆ°á»i thá»±c hiá»‡n vÃ  sá»‘ lÆ°á»£ng trÆ°á»›c/sau.

NgÆ°á»¡ng cáº¥u hÃ¬nh há»£p lá»‡: `0 <= ReorderPoint < MaximumStock`.

| Äiá»u kiá»‡n | Tráº¡ng thÃ¡i |
|---|---|
| `Quantity = 0` | `OUT_OF_STOCK` |
| `0 < Quantity <= ReorderPoint` | `LOW_STOCK` |
| `ReorderPoint < Quantity <= MaximumStock` | `NORMAL` |
| `Quantity > MaximumStock` | `OVERSTOCK` |

TÃ­nh tráº¡ng thÃ¡i táº¡i Business; UI hiá»ƒn thá»‹ káº¿t quáº£ báº±ng tiáº¿ng Viá»‡t. KhÃ´ng hard-code ngÆ°á»¡ng demo hoáº·c tÃ­nh láº¡i cÃ´ng thá»©c khÃ¡c á»Ÿ biá»ƒu Ä‘á»“.

## 8. Quy trÃ¬nh Git cá»§a nhÃ³m

`develop` là nhánh chính và m?c d?nh. Thành viên t?o `feature/*`, `fix/*` ho?c `chore/*` t? `develop`, push nhánh nhi?m v? và t?o Pull Request v? `develop`. C?n ít nh?t m?t thành viên khác ki?m tra tru?c khi merge. Không commit ho?c push tr?c ti?p vào `develop` trong công vi?c thông thu?ng.

### 8.1. Báº¯t Ä‘áº§u nhiá»‡m vá»¥

TrÆ°á»›c khi chuyá»ƒn nhÃ¡nh, cháº¡y `git status` vÃ  lÆ°u/commit cÃ´ng viá»‡c hiá»‡n táº¡i. KhÃ´ng pull khi cÃ²n thay Ä‘á»•i chÆ°a xá»­ lÃ½.

```powershell
git status
git switch develop
git pull --ff-only origin develop
git switch -c feature/khang-import
```

VÃ­ dá»¥ nhÃ¡nh:

- `feature/baoanh-database`
- `feature/baoanh-architecture`
- `feature/khang-import`
- `feature/khang-export`
- `feature/khang-inventory`
- `feature/khiem-ui`
- `feature/khiem-dashboard`
- `fix/export-stock-validation`

Không code tr?c ti?p trên `develop`. Không s?a nhánh dang du?c thành viên khác dùng.

### 8.2. PhÃ¡t triá»ƒn vÃ  commit

- LÃ m Ä‘Ãºng pháº¡m vi, commit theo tá»«ng thay Ä‘á»•i cÃ³ Ã½ nghÄ©a.
- Build vÃ  cháº¡y test liÃªn quan trÆ°á»›c khi push.
- Xem `git diff`; chá»‰ stage file thuá»™c nhiá»‡m vá»¥. TrÃ¡nh `git add .` khi repository cÃ²n thay Ä‘á»•i khÃ´ng liÃªn quan hoáº·c cáº¥u hÃ¬nh cÃ¡ nhÃ¢n.

VÃ­ dá»¥, thay Ä‘Æ°á»ng dáº«n báº±ng file thá»±c táº¿ Ä‘Ã£ sá»­a:

```powershell
git status
git diff
git add SmartInventoryManager.Business/Services/ImportService.cs
git commit -m "feat(import): implement stock import workflow"
```

### 8.3. Cáº­p nháº­t tá»« nhÃ¡nh tÃ­ch há»£p

Sau khi lÆ°u/commit cÃ´ng viá»‡c vÃ  working tree sáº¡ch:

```powershell
git fetch origin
git merge origin/develop
```

Náº¿u cÃ³ conflict:

1. Kiá»ƒm tra cÃ¡c file xung Ä‘á»™t; phá»‘i há»£p ngÆ°á»i phá»¥ trÃ¡ch khi cáº§n.
2. Sá»­a theo logic Ä‘Ãºng, khÃ´ng chá»n toÃ n bá»™ má»™t phÃ­a chá»‰ Ä‘á»ƒ háº¿t conflict.
3. Stage file Ä‘Ã£ xá»­ lÃ½ vÃ  hoÃ n táº¥t merge.
4. Build/test láº¡i.

KhÃ´ng dÃ¹ng `reset --hard` hoáº·c force push Ä‘á»ƒ giáº£i quyáº¿t conflict. HÆ°á»›ng dáº«n nÃ y dÃ¹ng merge; náº¿u nhÃ³m chá»n rebase thÃ¬ thá»‘ng nháº¥t trÆ°á»›c.

### 8.4. Push vÃ  táº¡o Pull Request

```powershell
git push -u origin feature/khang-import
```

Táº¡o PR trÃªn GitHub:

- **Base:** `develop`.
- **Compare:** nhÃ¡nh feature/fix Ä‘ang lÃ m.
- MÃ´ táº£ váº¥n Ä‘á», thay Ä‘á»•i, cÃ¡ch kiá»ƒm tra vÃ  pháº§n chÆ°a hoÃ n thÃ nh.
- Nhá» Ã­t nháº¥t má»™t thÃ nh viÃªn review; thay Ä‘á»•i liÃªn táº§ng cáº§n ngÆ°á»i phá»¥ trÃ¡ch liÃªn quan xem.
- Merge khi Ä‘Ã£ xá»­ lÃ½ Ã½ kiáº¿n vÃ  cÃ¡c kiá»ƒm tra yÃªu cáº§u Ä‘áº¡t.
- XÃ³a nhÃ¡nh feature sau merge khi nhÃ³m khÃ´ng cÃ²n cáº§n; khÃ´ng xÃ³a nhÃ¡nh cá»§a ngÆ°á»i khÃ¡c.

Không hu?ng d?n g?p `develop` sang `main` ho?c `master`.

## 9. Quy Æ°á»›c commit

| Prefix | Khi sá»­ dá»¥ng | VÃ­ dá»¥ Ä‘Ãºng vá»›i dá»± Ã¡n |
|---|---|---|
| `feat` | ThÃªm chá»©c nÄƒng | `feat(export): validate available stock` |
| `fix` | Sá»­a lá»—i | `fix(inventory): prevent duplicate stock updates` |
| `docs` | Sá»­a tÃ i liá»‡u | `docs: update team setup guide` |
| `style` | Äá»‹nh dáº¡ng, khÃ´ng Ä‘á»•i logic | `style: format C# source files` |
| `refactor` | TÃ¡i cáº¥u trÃºc, khÃ´ng Ä‘á»•i hÃ nh vi | `refactor(product): simplify product validation` |
| `perf` | Cáº£i thiá»‡n hiá»‡u nÄƒng | `perf(report): optimize inventory query` |
| `test` | ThÃªm/sá»­a kiá»ƒm thá»­ | `test(export): cover insufficient stock` |
| `chore` | Cáº¥u hÃ¬nh, dependency, báº£o trÃ¬ | `chore(agent): add shared project instructions` |

KhÃ´ng ghi thÃ´ng Ä‘iá»‡p chung chung nhÆ° `update`, `fix all`, `done`. KhÃ´ng Ä‘Æ°a máº­t kháº©u hoáº·c dá»¯ liá»‡u nháº¡y cáº£m vÃ o commit/PR.

## 10. HÆ°á»›ng dáº«n sá»­ dá»¥ng Copilot / AI Agent

### 10.1. Báº­t quy táº¯c dÃ¹ng chung

File `.github/copilot-instructions.md` Ä‘Æ°á»£c commit cÃ¹ng repository. Sau khi pull, má»—i thÃ nh viÃªn tá»± báº­t trÃªn Visual Studio cá»§a mÃ¬nh:

1. VÃ o **Tools â†’ Options**.
2. TÃ¬m **GitHub â†’ Copilot â†’ Copilot Chat**; giao diá»‡n má»›i cÃ³ thá»ƒ náº±m trong **All Settings**.
3. Báº­t **Enable custom instructions** hoáº·c tÃ¹y chá»n Ä‘á»c `.github/copilot-instructions.md` tÆ°Æ¡ng á»©ng.
4. Má»Ÿ cuá»™c trÃ² chuyá»‡n má»›i.
5. Kiá»ƒm tra **References** trong cÃ¢u tráº£ lá»i cÃ³ file hÆ°á»›ng dáº«n.

`.agent/RULE.md` lÃ  báº£n cÃ¡ nhÃ¢n, khÃ´ng tá»± thay tháº¿ file Copilot Ä‘á»c. Khi sá»­a quy táº¯c dÃ¹ng chung, táº¡o PR Ä‘á»ƒ nhÃ³m review; Ä‘á»“ng bá»™ báº£n cÃ¡ nhÃ¢n náº¿u Ä‘ang giá»¯. Báº£n dá»‹ch tiáº¿ng Viá»‡t trong Documentation cÃ³ thá»ƒ lÆ°u cÃ¹ng repo Ä‘á»ƒ tham kháº£o.

### 10.2. CÃ¡ch giao viá»‡c

Má»—i prompt cáº§n cÃ³:

- **NgÆ°á»i/pháº¡m vi:** lÃ m theo pháº§n cá»§a ai.
- **Má»¥c tiÃªu:** chá»©c nÄƒng cá»¥ thá»ƒ cáº§n hoÃ n thÃ nh.
- **File/contract:** file liÃªn quan, Ä‘áº§u vÃ o/Ä‘áº§u ra, máº«u Ä‘ang dÃ¹ng.
- **RÃ ng buá»™c:** giá»¯ cÃ´ng nghá»‡, quyá»n sá»­a, quy táº¯c nghiá»‡p vá»¥.
- **Kiá»ƒm tra:** build, test vÃ  tiÃªu chÃ­ Ä‘áº¡t.

Agent Ä‘Æ°á»£c Ä‘á»c táº§ng khÃ¡c Ä‘á»ƒ hiá»ƒu há»£p Ä‘á»“ng. Sá»­a sang pháº§n khÃ¡c hoáº·c thÃªm gÃ³i chÆ°a Ä‘Æ°á»£c duyá»‡t pháº£i Ä‘Æ°á»£c cho phÃ©p rÃµ rÃ ng. KhÃ´ng báº­t quyá»n rá»™ng chá»‰ Ä‘á»ƒ agent vÆ°á»£t qua quy táº¯c cá»§a nhÃ³m.

### 10.3. Prompt cho Khang

```text
LÃ m theo pháº¡m vi cá»§a Khang: Backend + Warehouse Operations.
Äá»c .github/copilot-instructions.md trÆ°á»›c khi sá»­a.

Má»¥c tiÃªu: triá»ƒn khai ExportService theo há»£p Ä‘á»“ng vÃ  mÃ´ hÃ¬nh Ä‘Ã£ cÃ³.
Chá»‰ sá»­a pháº§n Business vÃ  cÃ¡c test nghiá»‡p vá»¥ Ä‘Æ°á»£c giao.

YÃªu cáº§u:
- Kiá»ƒm tra sá»‘ lÆ°á»£ng há»£p lá»‡ vÃ  Ä‘á»§ tá»“n táº¡i Ä‘Ãºng kho.
- Má»—i phiáº¿u chá»‰ giáº£m tá»“n má»™t láº§n.
- Phiáº¿u, tá»“n vÃ  lá»‹ch sá»­ Ä‘Æ°á»£c lÆ°u nháº¥t quÃ¡n.
- PhÃ¡t sá»± kiá»‡n sau commit; UI refresh lá»—i khÃ´ng Ä‘Æ°á»£c xuáº¥t láº¡i.
- KhÃ´ng tá»± sá»­a Data, UI hoáº·c thÃªm thÆ° viá»‡n.

Kiá»ƒm tra: xuáº¥t 50 khi chá»‰ cÃ³ 20 pháº£i bá»‹ tá»« chá»‘i, khÃ´ng Ä‘á»•i tá»“n.
TrÆ°á»›c khi sá»­a, liá»‡t kÃª file dá»± kiáº¿n thay Ä‘á»•i rá»“i thá»±c hiá»‡n.
Náº¿u cáº§n thay Ä‘á»•i ngoÃ i pháº¡m vi, bÃ¡o há»£p Ä‘á»“ng cáº§n bá»• sung.
Sau cÃ¹ng, build/test phÃ¹ há»£p vÃ  bÃ¡o káº¿t quáº£ thá»±c táº¿.
```

### 10.4. Prompt cho Báº£o Anh

```text
LÃ m theo pháº¡m vi cá»§a Báº£o Anh: Database + Architecture.
Äá»c .github/copilot-instructions.md trÆ°á»›c khi sá»­a.

Má»¥c tiÃªu: hoÃ n thiá»‡n mÃ´ hÃ¬nh tá»“n theo sáº£n pháº©m vÃ  kho dá»±a trÃªn
schema vÃ  há»£p Ä‘á»“ng Ä‘Ã£ Ä‘Æ°á»£c nhÃ³m thá»‘ng nháº¥t.

Chá»‰ sá»­a Data vÃ  SQL script liÃªn quan. Báº£o Ä‘áº£m khÃ³a, FK,
constraint vÃ  cÆ¡ cháº¿ concurrency phÃ¹ há»£p. KhÃ´ng táº¡o Trigger
tá»± tÄƒng/giáº£m Inventory, khÃ´ng sá»­a Business/UI hoáº·c tá»± cháº¡y
script lÃ m thay Ä‘á»•i database tháº­t.

Náº¿u há»£p Ä‘á»“ng chung cáº§n Ä‘á»•i, nÃªu áº£nh hÆ°á»Ÿng Ä‘á»ƒ phá»‘i há»£p.
Build vÃ  kiá»ƒm tra phÃ¹ há»£p; bÃ¡o pháº§n chÆ°a kiá»ƒm chá»©ng.
```

### 10.5. Prompt cho KhiÃªm

```text
LÃ m theo pháº¡m vi cá»§a KhiÃªm: UI + Dashboard + Analytics.
Äá»c .github/copilot-instructions.md trÆ°á»›c khi sá»­a.

Má»¥c tiÃªu: hoÃ n thiá»‡n mÃ n hÃ¬nh tá»“n kho, dÃ¹ng Service/DTO Ä‘Ã£ cÃ³.
Chá»‰ sá»­a UI vÃ  pháº§n kiá»ƒm thá»­ giao diá»‡n liÃªn quan.

Hiá»ƒn thá»‹ sáº£n pháº©m, kho, sá»‘ lÆ°á»£ng, Ä‘Æ¡n vá»‹ vÃ  tráº¡ng thÃ¡i tráº£ vá»
tá»« Business. DÃ¹ng control WinForms/thÆ° viá»‡n Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t.
UI tiáº¿ng Viá»‡t, cÃ³ tÃ¬m kiáº¿m/lá»c phÃ¹ há»£p, tráº¡ng thÃ¡i táº£i/rá»—ng/lá»—i,
TabIndex há»£p lÃ½ vÃ  layout khÃ´ng cáº¯t chá»¯ khi Ä‘á»•i DPI.

KhÃ´ng truy váº¥n DbContext trá»±c tiáº¿p tá»« Form, khÃ´ng tÃ­nh láº¡i
StockStatus, khÃ´ng tá»± sá»­a Business/Data hoáº·c cÃ i gÃ³i UI má»›i.
Náº¿u Service cÃ²n thiáº¿u, bÃ¡o há»£p Ä‘á»“ng cáº§n cÃ³, khÃ´ng dÃ¹ng dá»¯ liá»‡u
giáº£ Ä‘á»ƒ tuyÃªn bá»‘ chá»©c nÄƒng Ä‘Ã£ hoÃ n thÃ nh.

Build, kiá»ƒm tra Designer vÃ  cháº¡y UI náº¿u cÃ³ mÃ´i trÆ°á»ng.
```

### 10.6. Kiá»ƒm tra code do AI táº¡o

- Xem danh sÃ¡ch file Ä‘á»•i vÃ  `git diff` trÆ°á»›c khi commit.
- Kiá»ƒm tra khÃ´ng sá»­a ngoÃ i phÃ¢n cÃ´ng hoáº·c cÃ i gÃ³i khÃ´ng Ä‘Æ°á»£c duyá»‡t.
- Kiá»ƒm tra khÃ´ng cÃ³ code giáº£, lá»—i bá»‹ nuá»‘t, máº­t kháº©u hard-code hoáº·c dá»¯ liá»‡u máº¥t khi tháº¥t báº¡i.
- Äá»‘i chiáº¿u Ä‘iá»u kiá»‡n tá»“n vÃ  transaction báº±ng test phÃ¹ há»£p.
- Build thÃ nh cÃ´ng khÃ´ng thay tháº¿ review nghiá»‡p vá»¥ hoáº·c kiá»ƒm tra giao diá»‡n.
- Náº¿u agent chÆ°a cháº¡y Ä‘Æ°á»£c kiá»ƒm tra, PR pháº£i nÃªu rÃµ giá»›i háº¡n Ä‘Ã³.

## 11. Kiá»ƒm thá»­ vÃ  Ä‘iá»u kiá»‡n trÆ°á»›c PR

Cháº¡y táº¡i thÆ° má»¥c solution:

```powershell
dotnet restore
dotnet build --no-restore
```

Khi Ä‘Ã£ cÃ³ project test Ä‘Æ°á»£c build vÃ  framework test Ä‘Æ°á»£c duyá»‡t:

```powershell
dotnet test --no-build
```

Náº¿u chÆ°a cÃ³ test, khÃ´ng coi viá»‡c lá»‡nh cháº¡y khÃ´ng lá»—i lÃ  báº±ng chá»©ng nghiá»‡p vá»¥ Ä‘Æ°á»£c kiá»ƒm thá»­. DÃ¹ng analyzer vÃ  `.editorconfig` cá»§a repository; chá»‰ cháº¡y kiá»ƒm tra Ä‘á»‹nh dáº¡ng khi SDK/cáº¥u hÃ¬nh há»— trá»£, khÃ´ng tá»± format toÃ n bá»™ code cá»§a Ä‘á»“ng Ä‘á»™i.

- Unit test: Business, validation, tÃ­nh tá»“n/tráº¡ng thÃ¡i, cáº£nh bÃ¡o, phÃ¢n quyá»n khi cÃ³ thá»ƒ kiá»ƒm tra Ä‘á»™c láº­p.
- Integration test: dÃ¹ng database test riÃªng cho truy váº¥n, lÆ°u dá»¯ liá»‡u, constraint, transaction vÃ  concurrency.
- KhÃ´ng dÃ¹ng EF InMemory Ä‘á»ƒ káº¿t luáº­n hÃ nh vi SQL Server Ä‘Ã£ Ä‘Ãºng.
- KhÃ´ng cháº¡y test ghi dá»¯ liá»‡u trÃªn database tháº­t.
- UI: kiá»ƒm tra má»Ÿ Designer, cháº¡y tÃ¡c vá»¥ bá»‹ áº£nh hÆ°á»Ÿng, bÃ n phÃ­m, tiáº¿ng Viá»‡t dÃ i, resize vÃ  scaling 100/125/150/200% khi cÃ³ mÃ´i trÆ°á»ng.

Checklist trÆ°á»›c PR:

- [ ] ÄÃºng pháº¡m vi; thay Ä‘á»•i liÃªn táº§ng Ä‘Ã£ Ä‘Æ°á»£c thá»‘ng nháº¥t.
- [ ] KhÃ´ng thÃªm cÃ´ng nghá»‡/dependency chÆ°a Ä‘Æ°á»£c duyá»‡t.
- [ ] Build Ä‘áº¡t; test liÃªn quan Ä‘áº¡t hoáº·c nÃªu rÃµ chÆ°a cháº¡y.
- [ ] KhÃ´ng cÃ³ lá»—i/cáº£nh bÃ¡o má»›i do thay Ä‘á»•i; lá»—i cÅ© Ä‘Æ°á»£c ghi rÃµ.
- [ ] KhÃ´ng commit bÃ­ máº­t, file mÃ¡y cÃ¡ nhÃ¢n hoáº·c tráº¡ng thÃ¡i agent.
- [ ] UI dÃ¹ng tiáº¿ng Viá»‡t; chÃº thÃ­ch `[COLOR CONFIG]` Ä‘Æ°á»£c giá»¯ náº¿u sá»­a mÃ u.
- [ ] PR mÃ´ táº£ hÃ nh vi thá»±c táº¿, khÃ´ng bÃ¡o chá»©c nÄƒng khung lÃ  hoÃ n thiá»‡n.

## 12. File khÃ´ng commit vÃ  cáº¥u hÃ¬nh cÃ¡ nhÃ¢n

`.gitignore` tá»‘i thiá»ƒu cáº§n phÃ¹ há»£p cáº¥u trÃºc hiá»‡n cÃ³:

```gitignore
.vs/
**/bin/
**/obj/
.agent/
*.user
*.suo
*.mdf
*.ldf
```

CÃ¡c file cáº¥u hÃ¬nh local chá»©a bÃ­ máº­t pháº£i Ä‘Æ°á»£c bá» qua theo tÃªn tháº­t nhÃ³m chá»n. KhÃ´ng bá» qua toÃ n bá»™ `.github/`: nhÃ³m cáº§n chia sáº» `.github/copilot-instructions.md` vÃ  cáº¥u hÃ¬nh cá»™ng tÃ¡c.

Náº¿u trÆ°á»›c Ä‘Ã¢y Ä‘Ã£ bá» qua file Copilot trong `.git/info/exclude`, bá» dÃ²ng Ä‘Ã³ khi nhÃ³m quyáº¿t Ä‘á»‹nh chia sáº» file qua Git. Ignore khÃ´ng tá»± ngá»«ng theo dÃµi file Ä‘Ã£ commit; xá»­ lÃ½ vá»›i ngÆ°á»i phá»¥ trÃ¡ch repository náº¿u cáº§n.

## 13. Má»™t sá»‘ lá»—i thÆ°á»ng gáº·p

| Váº¥n Ä‘á» | CÃ¡ch kiá»ƒm tra |
|---|---|
| KhÃ´ng tÃ¬m tháº¥y SDK / NETSDK1045 | Kiá»ƒm tra `dotnet --list-sdks`, target framework vÃ  global.json; cÃ i SDK Ä‘Ãºng |
| Restore tháº¥t báº¡i | Kiá»ƒm tra máº¡ng, nguá»“n NuGet vÃ  phiÃªn báº£n gÃ³i; khÃ´ng tá»± Ä‘á»•i stack |
| GÃ³i EF khÃ´ng tÆ°Æ¡ng thÃ­ch | Äá»‘i chiáº¿u target framework; cÃ¡c gÃ³i EF dÃ¹ng cÃ¹ng phiÃªn báº£n cá»¥ thá»ƒ |
| KhÃ´ng káº¿t ná»‘i SQL Server | Kiá»ƒm tra dá»‹ch vá»¥/instance, database, cÃ¡ch xÃ¡c thá»±c vÃ  cáº¥u hÃ¬nh á»©ng dá»¥ng |
| Thiáº¿u báº£ng hoáº·c cá»™t | Kiá»ƒm tra database/schema Ä‘Ã£ táº¡o Ä‘Ãºng báº£n, khÃ´ng tá»± cháº¡y migration chá»“ng script |
| Copilot chÆ°a tháº¥y quy táº¯c | Kiá»ƒm tra file á»Ÿ gá»‘c repository, tÃªn .md, báº­t custom instructions vÃ  má»Ÿ chat má»›i |
| Form Designer bÃ¡o lá»—i | Kiá»ƒm tra build, constructor vÃ  file Designer; khÃ´ng gá»i database trong luá»“ng thiáº¿t káº¿ Form |
| Git pull khÃ´ng fast-forward | Dá»«ng, kiá»ƒm tra nhÃ¡nh/lá»‹ch sá»­ vÃ  phá»‘i há»£p nhÃ³m; khÃ´ng force/reset Ä‘á»ƒ bá» lá»—i |

## 14. TÃ i liá»‡u tham kháº£o

- [Táº¡o á»©ng dá»¥ng Windows Forms vá»›i .NET](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/get-started/create-app-visual-studio)
- [CÃ i Ä‘áº·t Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/get-started/overview/install)
- [TÃ¹y chá»‰nh Copilot Chat trong Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-chat-context?view=visualstudio)

Má»i thay Ä‘á»•i vá» cÃ´ng nghá»‡, nghiá»‡p vá»¥ vÃ  phÃ¢n cÃ´ng dÃ¹ng chung cáº§n Ä‘Æ°á»£c cáº­p nháº­t trong tÃ i liá»‡u/quy táº¯c qua PR, trÃ¡nh Ä‘á»ƒ má»—i thÃ nh viÃªn Ã¡p dá»¥ng má»™t phiÃªn báº£n khÃ¡c nhau.

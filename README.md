<<<<<<< HEAD
# Estimate Pending – Cost Management System
### Tata Steel UISL | ASP.NET Core 3.1 MVC

---

## 📁 Project Structure

```
EstimateProject/
│
├── Controllers/
│   └── EstimateController.cs       ← All 7 actions: Import, Download, AddItem, Remove, Verify, Submit, CostBook
│
├── Data/
│   └── DatabaseHelper.cs           ← ADO.NET: stored procedures, transactions
│
├── Models/
│   └── EstimateModels.cs           ← EstimateRow, ViewModel, MaterialMasterRow, HistoryRow
│
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml          ← Navbar, footer, Bootstrap 4 layout
│   └── Estimate/
│       ├── Index.cshtml            ← Main page (toolbar + table)
│       ├── CostBook.cshtml         ← Standalone printable report (new tab)
│       └── History.cshtml          ← Submission history page
│
├── wwwroot/
│   ├── css/estimate.css            ← Full custom stylesheet (Tata Steel blue theme)
│   └── js/estimate.js              ← jQuery: Add/Remove rows, Verify, Submit, autocomplete
│
├── Scripts/
│   └── DatabaseSetup.sql           ← Full SQL: tables, stored procedures, seed data
│
├── appsettings.json                ← Connection string (update to match your server)
├── Startup.cs                      ← Services, Session, DI
├── Program.cs                      ← Host builder
└── EstimateProject.csproj          ← NuGet packages
```

---

## ⚙️ Setup Instructions

### Step 1 – Prerequisites
- Visual Studio 2019 (or later)
- .NET Core 3.1 SDK
- SQL Server 2019 (or SQL Server Express)
- SQL Server Management Studio (SSMS)

### Step 2 – Database Setup
1. Open **SSMS** and connect to your SQL Server instance.
2. Open `Scripts/DatabaseSetup.sql`
3. Run the entire script (F5 or Execute).
4. This creates:
   - Database: `EstimateDB`
   - Tables: `T_MaterialMaster`, `T_EstimateHeader`, `T_EstimateDetail`
   - Stored Procedures: `usp_SubmitEstimate`, `usp_SubmitEstimateDetail`, `usp_GetMaterialMaster`, `usp_GetEstimateHistory`
   - Seed data: 15 sample material master items

### Step 3 – Update Connection String
Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=EstimateDB;Trusted_Connection=True;"
}
```
Replace `YOUR_SERVER_NAME` with your SQL Server instance (e.g., `localhost`, `.\SQLEXPRESS`, `PC-NAME\SQLEXPRESS`).

### Step 4 – Restore NuGet Packages
In Visual Studio: **Tools → NuGet Package Manager → Restore** or run:
```bash
dotnet restore
```

Packages used:
| Package | Purpose |
|---------|---------|
| EPPlus 5.8.14 | Read/write Excel files |
| System.Data.SqlClient 4.8.5 | ADO.NET SQL Server |
| Newtonsoft.Json 13.0.3 | Session JSON serialization |
| iTextSharp 5.5.13.3 | PDF generation (optional) |

### Step 5 – Run the Project
Press **F5** in Visual Studio or:
```bash
dotnet run
```
Navigate to `https://localhost:5001` or `http://localhost:5000`

---

## 🚀 Feature Guide

### 1. Import Excel
- Click **Import Excel** → select a `.xlsx` file
- Expected columns: `Item Description | UOM | Quantity | Material Cost | Service Cost`
- Row 1 = headers, data starts from Row 2

### 2. Download Template
- Click **Download Template** to get a pre-formatted `.xlsx` with sample data

### 3. Add Item (Manual)
- Click **Add Item** to append a blank editable row
- Start typing in the Description field – it autocompletes from the Material Master

### 4. Remove
- Click ✕ on any row to delete it; serial numbers reindex automatically
- **Remove All** clears the entire session

### 5. Verify
- Validates each row for:
  - Correct formula: `TotalCost = (Qty × MaterialCost) + ServiceCost`
  - Quantity > 0
  - Cross-check with `T_MaterialMaster` (cost & UOM match)
- ✅ Green rows = verified | ❌ Red rows = mismatch with reason

### 6. Submit
- Saves data via stored procedures in a SQL transaction
- Displays success modal with Estimate Number and Total Cost
- Works in demo mode (memory only) if DB is unavailable

### 7. Cost Book
- Opens in a **new browser tab**
- Shows full estimate table + isolated mismatch section
- Print-ready (Ctrl+P or the Print button)

---

## 🗄️ Database Schema

```sql
T_MaterialMaster   -- Standard rates (15 seeded items)
T_EstimateHeader   -- One row per submission (EstimateNo, TotalCost, Status)
T_EstimateDetail   -- Line items per estimate (linked by EstimateID)
```

---

## 🎨 UI Theme
- **Colors**: Tata Steel blue (`#00467f`) + amber accent (`#f5a623`)
- **Fonts**: Barlow + Barlow Condensed (Google Fonts)
- **Framework**: Bootstrap 4.6 + jQuery 3.6
- **Icons**: Font Awesome 6

---

## 🛠️ Technologies Used
| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 3.1 MVC |
| Database | SQL Server 2019 |
| ORM/DAL | ADO.NET (raw SQL + stored procedures) |
| Excel | EPPlus 5 |
| Frontend | HTML5, CSS3, Bootstrap 4, jQuery |
| Session | ASP.NET Core Session (memory-backed) |
| IDE | Visual Studio 2019 |
=======
# EstimateProject
Hello My name is Abhishek Das a student from Siksha O Anusandhan, Institute Of Technical Education &amp; Research , pursuing Btech in Computer Science &amp; Information Technology, and this my internship project tilted "Estimate Project" which i did in TATAUISL during my internship days  and these are the files crated and used to build this project.
>>>>>>> afc7edfe1da7225f5f1be66c163f71daafccc3ce

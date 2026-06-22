# ALNOUR Invoice Manager Pro v2

إصلاح جوهري: لا يوجد بحث LIKE عام عند التنفيذ. البرنامج يختار الصنف من أصناف الفاتورة فقط، ثم يعتمد على CLS_ID و SP_S_ID في المعاينة والتنفيذ.

## التشغيل

```powershell
cd C:\Users\iPharmEGY\Downloads\ALNOUR_Invoice_Manager_Pro_v2\ALNOUR_Invoice_Manager_Pro_v2
dotnet restore
dotnet run
```

## إنشاء EXE

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

## الجديد
- ComboBox الفواتير يعرض: رقم الفاتورة | التاريخ | الإجمالي.
- ComboBox الأصناف يعرض أصناف الفاتورة فقط.
- السعر الجديد يقترح السعر الافتراضي إن وجد، ثم السعر الحالي، ثم أسعار تاريخية.
- رسالة نجاح ختامية احترافية.
- منع التنفيذ إذا لم يكن هناك سطر واحد فقط.

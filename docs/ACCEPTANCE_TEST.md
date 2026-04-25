# Acceptance Test Procedure

**Project:** SilentPrintBridge  
**Version:** 1.0.0  
**Test Date:** _____________  
**Tester:** _____________  
**Result:** ☐ PASS  ☐ FAIL

---

## Test Objective

Verify that SilentPrintBridge enables silent receipt printing from Chrome and Edge browsers to an Epson TM-series thermal printer without showing Windows print dialogs.

**Success Criteria:**
- Receipt prints within 2 seconds
- No Windows print dialog appears
- No print preview window
- Paper cuts automatically (if enabled)
- Works in both Chrome and Edge

---

## Prerequisites

### Hardware
- ☐ Windows 10 or Windows 11 PC
- ☐ Epson TM-T20III or compatible thermal receipt printer
- ☐ Printer connected via USB or network
- ☐ Printer powered on
- ☐ Receipt paper loaded

### Software
- ☐ Epson Advanced Printer Driver (APD) installed
- ☐ SilentPrintBridge service installed
- ☐ Chrome browser installed
- ☐ Edge browser installed

### Configuration
- ☐ Printer appears in Windows "Printers & scanners"
- ☐ Windows test page prints successfully
- ☐ Printer name configured in `appsettings.json`
- ☐ SilentPrintBridge service is running

---

## Pre-Test Verification

### Step 1: Verify Printer Installation

1. Open **Settings → Devices → Printers & scanners**
2. Locate your Epson printer
3. Click printer name → **Manage**
4. Click **Print a test page**

**Expected Result:**  
☐ Test page prints successfully  
☐ Paper cuts (if printer supports auto-cut)

**Actual Result:**  
_____________________________________________

---

### Step 2: Verify Service Status

Open PowerShell and run:

```powershell
Get-Service SilentPrintBridge
```

**Expected Result:**  
☐ Status: Running  
☐ StartType: Automatic

**Actual Result:**  
_____________________________________________

---

### Step 3: Verify Printer Configuration

Open PowerShell and run:

```powershell
cd "C:\Program Files\SilentPrintBridge"
.\SilentPrintBridge.exe --list-printers
```

**Expected Result:**  
☐ Your Epson printer appears in the list  
☐ Printer name matches configuration

**Actual Result:**  
_____________________________________________

**Configured Printer Name:**  
_____________________________________________

---

### Step 4: Test Command Line Print

Run:

```powershell
cd "C:\Program Files\SilentPrintBridge"
.\SilentPrintBridge.exe --test-print
```

**Expected Result:**  
☐ Message: "SUCCESS: Printed X bytes"  
☐ Test receipt prints  
☐ Paper cuts (if enabled)  
☐ No Windows dialog

**Actual Result:**  
_____________________________________________

---

## Main Acceptance Tests

### Test 1: Health Check (Chrome)

1. Open Chrome browser
2. Navigate to: `file:///C:/Users/user/Desktop/PrinterApp/samples/sample.html`
   (Adjust path to your actual location)
3. Click **"Health Check"** button

**Expected Result:**  
☐ Response shows green success box  
☐ `"ok": true`  
☐ `"printerConfigured": true`  
☐ `"printerName"` matches your printer  
☐ No errors in browser console (F12)

**Actual Result:**  
_____________________________________________

**Screenshot:** ☐ Attached

---

### Test 2: List Printers (Chrome)

1. Click **"List Printers"** button

**Expected Result:**  
☐ Response shows list of printers  
☐ Your Epson printer is in the list  
☐ Default printer marked if applicable

**Actual Result:**  
_____________________________________________

---

### Test 3: Built-In Test Receipt (Chrome)

1. Ensure printer is ready (online, paper loaded)
2. Click **"Built-In Test Receipt"** button
3. Observe printer behavior
4. Note time from click to print completion

**Expected Result:**  
☐ Receipt prints within 2 seconds  
☐ No Windows print dialog appears  
☐ No print preview window  
☐ Receipt contains:
  - "SilentPrintBridge" title
  - "Test Receipt"
  - Current date/time
  - Printer name
  - Sample items
  - "Thank you!" footer
☐ Paper cuts automatically (if enabled)  
☐ JSON response shows `"success": true`

**Actual Result:**  
☐ Print time: _______ seconds  
☐ Dialog appeared: YES / NO  
☐ Receipt printed: YES / NO  
☐ Paper cut: YES / NO  

**Issues:**  
_____________________________________________

**Screenshot:** ☐ Attached

---

### Test 4: Plain Text Print (Chrome)

1. Click **"Print Plain Text"** button
2. Observe printer behavior

**Expected Result:**  
☐ Receipt prints within 2 seconds  
☐ No Windows dialog  
☐ Receipt contains sample items and total  
☐ Paper cuts  
☐ JSON response shows `"success": true`

**Actual Result:**  
_____________________________________________

---

### Test 5: Custom Text Print (Chrome)

1. Enter custom text in the text area:
   ```
   Custom Test Receipt
   Line 1
   Line 2
   Total: $99.99
   ```
2. Click **"Print Custom Text"** button

**Expected Result:**  
☐ Receipt prints with your custom text  
☐ No Windows dialog  
☐ Paper cuts  
☐ JSON response shows `"success": true`

**Actual Result:**  
_____________________________________________

---

### Test 6: ESC/POS Print (Chrome)

1. Click **"Print ESC/POS"** button
2. Observe printer behavior

**Expected Result:**  
☐ Receipt prints with formatted content  
☐ Title is centered and bold  
☐ Total is right-aligned and bold  
☐ No Windows dialog  
☐ Paper cuts  
☐ JSON response shows `"success": true`

**Actual Result:**  
_____________________________________________

---

### Test 7: Repeat Tests in Edge

1. Open Microsoft Edge browser
2. Navigate to `sample.html`
3. Repeat Tests 1-6

**Results:**

| Test | Result | Notes |
|------|--------|-------|
| Health Check | ☐ PASS ☐ FAIL | |
| List Printers | ☐ PASS ☐ FAIL | |
| Built-In Test | ☐ PASS ☐ FAIL | |
| Plain Text | ☐ PASS ☐ FAIL | |
| Custom Text | ☐ PASS ☐ FAIL | |
| ESC/POS | ☐ PASS ☐ FAIL | |

**Overall Edge Result:** ☐ PASS ☐ FAIL

---

### Test 8: Error Handling

1. Stop the SilentPrintBridge service:
   ```powershell
   Stop-Service SilentPrintBridge
   ```
2. In browser, click **"Health Check"**

**Expected Result:**  
☐ Error message appears  
☐ User-friendly error displayed  
☐ No browser crash

3. Start service again:
   ```powershell
   Start-Service SilentPrintBridge
   ```
4. Click **"Health Check"** again

**Expected Result:**  
☐ Service responds successfully  
☐ Connection restored

**Actual Result:**  
_____________________________________________

---

### Test 9: Invalid Printer Name

1. In browser, enter invalid printer name: "NonExistentPrinter"
2. Click **"Built-In Test Receipt"**

**Expected Result:**  
☐ Error response: "Printer 'NonExistentPrinter' not found"  
☐ Error code: "PRINTER_NOT_FOUND"  
☐ No crash or hang

**Actual Result:**  
_____________________________________________

---

### Test 10: Multiple Rapid Prints

1. Click **"Built-In Test Receipt"** button
2. Immediately click it again (3 times total)
3. Observe printer behavior

**Expected Result:**  
☐ All 3 receipts print  
☐ Prints are sequential (not overlapped)  
☐ No errors  
☐ No service crash

**Actual Result:**  
☐ Receipts printed: _______  
☐ Issues: _____________________________________________

---

## Performance Tests

### Test 11: Print Speed Measurement

1. Click **"Built-In Test Receipt"**
2. Start timer when button is clicked
3. Stop timer when paper finishes cutting

**Results:**

| Attempt | Time (seconds) | Success |
|---------|----------------|---------|
| 1 | | ☐ YES ☐ NO |
| 2 | | ☐ YES ☐ NO |
| 3 | | ☐ YES ☐ NO |
| Average | | |

**Target:** Under 2 seconds  
**Result:** ☐ PASS ☐ FAIL

---

### Test 12: Long Receipt

1. Enter long text (50+ lines) in custom text area
2. Click **"Print Custom Text"**

**Expected Result:**  
☐ Receipt prints completely  
☐ All lines printed  
☐ No truncation  
☐ Paper cuts at end

**Actual Result:**  
_____________________________________________

---

## Integration Test (Optional)

### Test 13: Integration with Your Application

If you have integrated SilentPrintBridge into your web application:

1. Open your application in Chrome
2. Trigger a print action
3. Observe behavior

**Expected Result:**  
☐ Receipt prints silently  
☐ No Windows dialog  
☐ Print completes within 2 seconds  
☐ Application continues normally

**Actual Result:**  
_____________________________________________

---

## Final Verification

### Checklist

☐ All Chrome tests passed  
☐ All Edge tests passed  
☐ No Windows print dialogs appeared  
☐ Print speed under 2 seconds  
☐ Paper cuts automatically  
☐ Error handling works correctly  
☐ Service restarts successfully  
☐ Configuration can be changed  
☐ Logs are being written  
☐ Documentation is clear and accurate

---

## Test Summary

**Total Tests:** 13  
**Passed:** _______  
**Failed:** _______  
**Skipped:** _______

**Overall Result:** ☐ PASS ☐ FAIL

---

## Issues Found

| # | Test | Issue Description | Severity | Status |
|---|------|-------------------|----------|--------|
| 1 | | | ☐ Critical ☐ Major ☐ Minor | ☐ Open ☐ Resolved |
| 2 | | | ☐ Critical ☐ Major ☐ Minor | ☐ Open ☐ Resolved |
| 3 | | | ☐ Critical ☐ Major ☐ Minor | ☐ Open ☐ Resolved |

---

## Troubleshooting Performed

If any tests failed, document troubleshooting steps taken:

_____________________________________________
_____________________________________________
_____________________________________________

---

## System Information

**Windows Version:** _____________________________________________  
**Printer Model:** _____________________________________________  
**Printer Driver:** _____________________________________________  
**Chrome Version:** _____________________________________________  
**Edge Version:** _____________________________________________  
**SilentPrintBridge Version:** 1.0.0

---

## Attachments

☐ Screenshots of successful prints  
☐ Screenshots of any errors  
☐ Sample printed receipts  
☐ Log file excerpts (if issues occurred)  
☐ Configuration file (appsettings.json)

---

## Sign-Off

**Tester Name:** _____________________________________________  
**Tester Signature:** _____________________________________________  
**Date:** _____________________________________________

**Client Representative:** _____________________________________________  
**Signature:** _____________________________________________  
**Date:** _____________________________________________

---

## Notes

_____________________________________________
_____________________________________________
_____________________________________________
_____________________________________________

---

## Acceptance Decision

Based on the test results above:

☐ **ACCEPTED** - System meets all requirements and is ready for production use

☐ **ACCEPTED WITH MINOR ISSUES** - System is acceptable with noted minor issues to be addressed

☐ **REJECTED** - Critical issues must be resolved before acceptance

**Decision By:** _____________________________________________  
**Date:** _____________________________________________

---

**End of Acceptance Test**

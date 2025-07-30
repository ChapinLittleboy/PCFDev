using BlazorServerDatagridApp2.Data;
using BlazorServerDatagridApp2.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using System.Net;
using System.Net.Mail;
using PointF = Syncfusion.Drawing.PointF;
using RectangleF = Syncfusion.Drawing.RectangleF;
using SizeF = Syncfusion.Drawing.SizeF;




namespace BlazorServerDatagridApp2.Services;

public class ExportService
{
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExportService> _logger;


    public ExportService(IWebHostEnvironment hostingEnvironment, IConfiguration configuration, ILogger<ExportService> logger)
    {
        _hostingEnvironment = hostingEnvironment;
        _configuration = configuration;
        _logger = logger;

    }

    public MemoryStream CreatePDF()
    {
        PdfDocument document = new PdfDocument();
        PdfPage currentPage = document.Pages.Add();
        Syncfusion.Drawing.SizeF clientSize = currentPage.GetClientSize();
        FileStream imageStream = new FileStream(_hostingEnvironment.WebRootPath + "//images/pdfheader.png", FileMode.Open, FileAccess.Read);
        PdfImage banner = new PdfBitmap(imageStream);
        SizeF bannerSize = new SizeF(500, 50);
        PointF bannerLocation = new PointF(0, 0);
        PdfGraphics graphics = currentPage.Graphics;
        graphics.DrawImage(banner, bannerLocation, bannerSize);
        // PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 20, PdfFontStyle.Bold);
        //var headerText = new PdfTextElement("INVOICE", font, new PdfSolidBrush(Color.FromArgb(1, 53, 67, 168)));
        // headerText.StringFormat = new PdfStringFormat(PdfTextAlignment.Right);
        // PdfLayoutResult result = headerText.Draw(currentPage, new PointF(clientSize.Width - 25, iconLocation.Y + 10));

        MemoryStream stream = new MemoryStream();
        document.Save(stream);
        document.Close(true);
        stream.Position = 0;

        return stream;
    }

    public MemoryStream CreatePCFPDF(PCFHeaderDTO header)
    {
        ;
        PdfDocument document = new PdfDocument();
        PdfPage page = document.Pages.Add();
        PdfGraphics graphics = page.Graphics;
        SizeF clientSize = page.GetClientSize();

        // Draw banner image
        using (FileStream imageStream = new FileStream(_hostingEnvironment.WebRootPath + "//images/pdfheader.png", FileMode.Open, FileAccess.Read))
        {
            PdfImage banner = new PdfBitmap(imageStream);
            SizeF bannerSize = new SizeF(500, 50);
            PointF bannerLocation = new PointF(0, 0);
            graphics.DrawImage(banner, bannerLocation, bannerSize);
        }

        // Define fonts
        PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
        PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        PdfFont regularFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        PdfFont infoFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
        PdfFont smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);

        // Define margins
        float marginX = 40;
        float yPosition = 70; // Start a little lower after the banner

        // Draw document title
        graphics.DrawString("Contracted Pricing Notification", titleFont, PdfBrushes.Black, new PointF(marginX, yPosition));
        yPosition += 25;



        // Draw customer information
        graphics.DrawString($"Customer: [{header.CustomerNumber}] {header.CustomerName}", headerFont, PdfBrushes.Black, new PointF(marginX, yPosition));
        yPosition += 20;

        // Draw customer bill to information

        var format3 = new PdfStringFormat
        {
            WordWrap = PdfWordWrapType.Word
        };

        var element = new PdfTextElement(
            $"Address: {header.CustomerInfo.BillToAddress1} {header.CustomerInfo.BillToAddress2} {header.CustomerInfo.BillToCity}, {header.CustomerInfo.BillToState} {header.CustomerInfo.BillToZip}",
            infoFont,
            PdfBrushes.Black
        )
        {
            StringFormat = format3
        };

        float availableWidth = page.GetClientSize().Width - 2 * marginX;
        var bounds = new RectangleF(marginX, yPosition, availableWidth, page.GetClientSize().Height - yPosition);

        // Draw() returns a PdfTextLayoutResult with the exact rectangle it filled
        PdfTextLayoutResult result = (PdfTextLayoutResult)element.Draw(page, bounds);           // :contentReference[oaicite:2]{index=2}

        yPosition = result.Bounds.Bottom + 5;




        // Draw PCF info


        // Draw effective date range
        string effectiveDateString = $"Approved prices for dates {header.StartDate:MM/dd/yyyy} through {header.EndDate:MM/dd/yyyy}";
        graphics.DrawString(effectiveDateString, infoFont, PdfBrushes.Black, new PointF(marginX, yPosition));
        yPosition += 15;
        string paymentTerms = !string.IsNullOrWhiteSpace(header.PromoPaymentTermsText)
            ? header.PromoPaymentTermsText
            : header.CustomerInfo.PaymentTermsDescription;



        string paymentTermsLabel = "Payment Terms:";

        if (!string.IsNullOrWhiteSpace(header.PromoPaymentTerms) && header.PromoPaymentTermsText != header.CustomerInfo.PaymentTermsDescription)
        {
            paymentTermsLabel = "Promo Payment Terms:";
        }
        else
        {
            paymentTermsLabel = "[Standard] Payment Terms:";
        }






        graphics.DrawString($"{paymentTermsLabel} {paymentTerms}", infoFont, PdfBrushes.Black, new PointF(marginX, yPosition));
        yPosition += 15;


        int promoFrtMinInt = FreightComparer.ConvertToInt(header.FreightMinimums);  // or should this be header.PromoFreightMinimums ?
        int stdFrtMinInt = (int)header.CustomerInfo.FreightMinimums;


        string freightTerms = !string.IsNullOrWhiteSpace(header.FreightTerms)
            ? header.FreightTerms
            : header.CustomerInfo.FreightTerms;

        if (promoFrtMinInt > 0)
        {
            freightTerms = freightTerms + " with Min Order $" + promoFrtMinInt.ToString("N0");
            ;
        }

        string freightTermsLabel = "Freight Terms:";

        if (!FreightComparer.AreFreightTermsEqual(header.FreightTerms, header.CustomerInfo.FreightTerms,
                FreightComparer.AreFreightMinimumsEqual(header.FreightMinimums, header.CustomerInfo.FreightMinimums.ToString())))
        {
            freightTermsLabel = "Promo Freight Terms:";
        }
        else
        {
            freightTermsLabel = "[Standard] Freight Terms:";
        }




        // string freightTerms = header.FreightTerms ?? header.CustomerInfo.FreightTerms;
        graphics.DrawString($"{freightTermsLabel} {freightTerms}", infoFont, PdfBrushes.Black, new PointF(marginX, yPosition));
        yPosition += 15;




        if (!string.IsNullOrWhiteSpace(header.GeneralNotes))
        {
            string generalNotes = header.GeneralNotes;

            RectangleF headerBounds = new RectangleF(marginX, yPosition, clientSize.Width - (2 * marginX), 70);
            PdfStringFormat format = new PdfStringFormat { LineSpacing = 3, WordWrap = PdfWordWrapType.Word };
            graphics.DrawString($"Notes: {generalNotes}", smallFont, PdfBrushes.Black, headerBounds, format);
            yPosition += 70; // More spacing for clarity
        }


        // string disclaimer = "*** This document is for reference only and does not replace signed contract ***";
        //  graphics.DrawString($"{disclaimer}", smallFont, PdfBrushes.Black, new PointF(marginX, yPosition));
        //  yPosition += 15;

        string contactString = string.Empty;

        if (!string.IsNullOrWhiteSpace(header.CustomerInfo.SalesManagerName) &&
            !string.IsNullOrWhiteSpace(header.CustomerInfo.SalesManagerEmail))
        {
            contactString =
                $@"Contact {header.CustomerInfo.SalesManagerName} at {header.CustomerInfo.SalesManagerEmail} if you have any questions.";
        }
        else
        {
            contactString = string.Empty;
        }



        // Draw contract details

        string disclaimer = @"
All prices and product details listed in this agreement are based on the most current information available at the time of issuance. 
While we strive for accuracy, typographical or clerical errors may occur. In the event of such errors, we reserve
the right to correct the pricing or product information at our discretion.

Prices and product availability are also subject to change at any time due to market conditions, supplier adjustments, or other unforeseen factors. Should any changes be necessary, we will provide notice as promptly as possible. This agreement is not intended to create any binding obligation beyond the terms and conditions set forth herein.
";



        string headerText =
            $"*** This document is for reference only and does not replace signed contract ***" +
            $"\n{disclaimer} " +
            $"\n{contactString}" +
            $"\nReference: {header.PCFTypeDescription} PCF {header.PcfNumber}";

        RectangleF headerBounds2 = new RectangleF(marginX, yPosition, clientSize.Width - (2 * marginX), 165);
        PdfStringFormat format2 = new PdfStringFormat { LineSpacing = 3, WordWrap = PdfWordWrapType.Word };
        graphics.DrawString(headerText, smallFont, PdfBrushes.Black, headerBounds2, format2);
        yPosition += 160; // More spacing for clarity




        /*

      // Draw "Terms & Conditions" Header
      // graphics.DrawString("Terms & Conditions:", headerFont, PdfBrushes.Black, new PointF(marginX, yPosition));
      // yPosition += 15;

      // Draw the Terms & Conditions as a **proper bulleted list**
      string[] terms =
      {
  "Prices listed are valid only during the effective date range and may be subject to review upon expiration.",
  "Any modifications to this agreement require written approval from both parties.",
  "Standard terms and conditions of sale apply unless otherwise specified."
};

      float bulletSize = 8; // Bullet point size
      float bulletIndent = 5;  // Distance from margin to bullet
      float textIndent = marginX + bulletSize + bulletIndent;  // Start of text after the bullet
      float lineHeight = 16; // Line spacing for bullets


      PdfUnorderedList bulletList = new PdfUnorderedList
      {
          Marker = { Style = PdfUnorderedMarkerStyle.Asterisk, }, // Ensures standard bullet points
          Font = regularFont,
          StringFormat = format,
          Indent = 10, // Indent for bullet points
          TextIndent = 10 // Additional space between bullet and text
      };

      foreach (var term in terms)
      {
          // Manually draw a circle bullet before each term

          bulletList.Items.Add(term);


      }
      //bulletList.Draw(page, new RectangleF(marginX, yPosition, clientSize.Width - marginX, clientSize.Height - yPosition));

      // **Additional spacing before the pricing table**
      yPosition += 25;

      */





        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Create the data table
        PdfGrid grid = new PdfGrid();
        grid.Columns.Add(3);
        grid.Headers.Add(1);

        PdfGridRow headerRow = grid.Headers[0];
        headerRow.Cells[0].Value = "Item Number";
        headerRow.Cells[1].Value = "Description";
        headerRow.Cells[2].Value = "Price";





        foreach (var item in header.PCFLines)
        {
            PdfGridRow row = grid.Rows.Add();

            row.Cells[0].Value = item.ItemNum;
            row.Cells[1].Value = item.ItemDesc;
            row.Cells[2].Value = item.ProposedPrice.ToString("C2");
        }


        // grid.ApplyBuiltinStyle(PdfGridBuiltinStyle.GridTable4Accent1);
        grid.ApplyBuiltinStyle(PdfGridBuiltinStyle.PlainTable3);

        grid.RepeatHeader = true;
        grid.Draw(page, new PointF(0, yPosition + 20));

        MemoryStream stream = new MemoryStream();
        document.Save(stream);
        document.Close(true);
        stream.Position = 0;

        return stream;
    }



    public void SendPcfPdfEmailWithAttachment(PCFHeaderDTO header, string EmailAddress, string CCEmailAddress = null)
    {
        // Generate the PDF
        MemoryStream pdfStream = CreatePCFPDF(header);

        var subject = $"PCF: {header.PcfNumber} for Customer: {header.CustomerNumber} {header.CustomerName} has been approved";
        var filename = $"PCF {header.PcfNumber}_{header.CustomerNumber}.pdf";
        var body = @"
            <p>We are pleased to inform you that the Pricing Control Form has been approved. Please find the attached document for your records.</p>
            <p>Please reach out to us if you have any questions after reviewing the PCF.</p>
            <p></p>
            <p>Best regards,</p>
            <p>Your friendly SalesOps team</p>
        ";

        // Create the email message
        MailMessage mail = new MailMessage();
        mail.From = new MailAddress("Sales-Ops@chapinmfg.com");

        // Normalize email list by replacing semicolons with commas
        string normalizedEmailList = EmailAddress.Replace(";", ",");

        // Add recipients (comma-separated)
        mail.To.Add(normalizedEmailList);

        // Add CC recipients if provided
        if (!string.IsNullOrEmpty(CCEmailAddress))
        {
            string normalizedCCEmailList = CCEmailAddress.Replace(";", ",");
            mail.CC.Add(normalizedCCEmailList);
        }

        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        // Attach the PDF
        pdfStream.Position = 0; // Reset the stream position to the beginning
        Attachment attachment = new Attachment(pdfStream, filename, "application/pdf");
        mail.Attachments.Add(attachment);

        // Configure the SMTP client
        var smtpPassword = _configuration["SmtpSettings:Password"]; // reads from appsettings.json or secrets.json in my APPDATA folder for Dev only
                                                                    // _logger.LogInformation("The smtpPassword appsettings or secrets is {smtpPassword}", smtpPassword);

        if (string.IsNullOrEmpty(smtpPassword))
        {
            smtpPassword = Environment.GetEnvironmentVariable("SMTPpassword"); // set in IIS under configuration editor
            _logger.LogInformation("The Env smtpPassword is {smtpPassword}", smtpPassword);

            smtpPassword = string.Empty;
            // _logger.LogInformation("The  Env override smtpPassword is {smtpPassword}", smtpPassword);

        }
        if (string.IsNullOrEmpty(smtpPassword))
        {
            smtpPassword = "D,$k3brpg8qrJ;_~";
            //  _logger.LogInformation("The hardcodded smtpPassword is {smtpPassword}", smtpPassword);

        }
        var smtpClient = new SmtpClient("CIIEXCH16")
        {
            Port = 25, // Typically, port 25 is used for Exchange Server
            Credentials = new NetworkCredential("Administrator", smtpPassword),
            EnableSsl = false, // Usually, SSL is not used for local Exchange Servers
        };

        // _logger.LogInformation("The final smtpPassword is {smtpPassword}", smtpPassword);

        // Send the email
        try
        {
            smtpClient.Send(mail);
        }
        catch (Exception ex)
        {
            // Handle the exception (log it, show a message, etc.)
            Console.WriteLine("Error sending email: " + ex.Message);
        }
        finally
        {
            // Clean up
            attachment.Dispose();
            pdfStream.Dispose();
        }
    }
}

using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using Spire.Doc.Formatting;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Xls;
using System.Drawing;
using Paragraph = Spire.Doc.Documents.Paragraph;
using Section = Spire.Doc.Section;

namespace AppFunctions.Helper
{
    public class ManagementDocs
    {
        //Funcion que inserta el radicado enviado en el doc o docx        
        public static string InsertRadicadeInDoc(string pathDocumento, string radicado)
        {
            Document document = new Document();

            document.LoadFromFile(pathDocumento, Spire.Doc.FileFormat.Doc);
            Paragraph paraInserted = new Paragraph(document);
            document.Sections[0].Paragraphs.Insert(0, paraInserted);

            float pageWidth = document.Sections[0].PageSetup.PageSize.Width - 4;
            float width = 8 * (radicado.Length + 12);
            float posX = (pageWidth / 3) * 2 - (width / 2);
            while (posX + width > pageWidth)
            {
                posX -= 10;
            }

            TextBox textBox = paraInserted.AppendTextBox(width, 20);
            Paragraph par1 = textBox.Body.AddParagraph();
            textBox.Format.TextWrappingStyle = TextWrappingStyle.InFrontOfText;
            textBox.Format.VerticalOrigin = VerticalOrigin.Margin;
            textBox.Format.VerticalPosition = -88;
            textBox.Format.HorizontalOrigin = HorizontalOrigin.Margin;
            textBox.Format.HorizontalPosition = posX;
            textBox.Format.LineColor = Color.White;
            textBox.Format.TextWrappingStyle = TextWrappingStyle.InFrontOfText;
            par1.AppendText("Radicado: " + radicado);

            document.SaveToFile($@"{pathDocumento}", Spire.Doc.FileFormat.Docx);

            document.Close();

            return $@"{pathDocumento}";

        }

        //Funcion que inserta el radicado enviado en el pdf        
        public static string InsertRadicadeInPdf(string pathDocumento, string radicado)
        {
            PdfDocument pdfDocument = new PdfDocument();
            pdfDocument.LoadFromFile(pathDocumento, Spire.Pdf.FileFormat.PDF);
            PdfPageBase page = pdfDocument.Pages[0];

            PdfFont font = new PdfFont(PdfFontFamily.Helvetica, 11f);
            PdfStringFormat rightAlignment = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Middle);
            PdfSolidBrush brush = new PdfSolidBrush(Color.Black);

            page.Canvas.DrawString("Radicado: " + radicado, font, brush, 525, 16, rightAlignment);

            pdfDocument.SaveToFile($@"{pathDocumento}", Spire.Pdf.FileFormat.PDF);

            return $@"{pathDocumento}";
        }


        //Funcion que inserta el radicado enviado en el doc o docx        
        public static string InsertDateInDoc(string pathDocumento, string fecha)
        {
            Document document = new Document();

            document.LoadFromFile(pathDocumento, Spire.Doc.FileFormat.Doc);
            Paragraph paraInserted = new Paragraph(document);
            document.Sections[0].Paragraphs.Insert(0, paraInserted);

            ParagraphStyle style = new ParagraphStyle(document);
            style.Name = "FontDateStyle";
            style.CharacterFormat.FontName = "Arial";
            style.CharacterFormat.FontSize = 12;
            document.Styles.Add(style);

            Spire.Doc.Fields.TextBox textBox = paraInserted.AppendTextBox(260, 28);
            Paragraph par1 = textBox.Body.AddParagraph();
            par1.ApplyStyle(style.Name);

            textBox.Format.TextWrappingStyle = TextWrappingStyle.InFrontOfText;
            textBox.Format.VerticalOrigin = VerticalOrigin.Margin;
            textBox.Format.VerticalPosition = 60;
            textBox.Format.HorizontalOrigin = HorizontalOrigin.Margin;
            textBox.Format.HorizontalPosition = 0;
            textBox.Format.LineColor = Color.White;
            textBox.Format.TextWrappingStyle = TextWrappingStyle.InFrontOfText;
            par1.AppendText("Fecha: " + fecha);

            document.SaveToFile($@"{pathDocumento}", Spire.Doc.FileFormat.Docx);

            document.Close();

            return $@"{pathDocumento}";

        }

        //Funcion que inserta el radicado enviado en el pdf        
        public static string InsertDateInPdf(string pathDocumento, string fecha)
        {
            PdfDocument pdfDocument = new PdfDocument();
            pdfDocument.LoadFromFile(pathDocumento, Spire.Pdf.FileFormat.PDF);
            PdfPageBase page = pdfDocument.Pages[0];

            PdfFont font = new PdfFont(PdfFontFamily.Helvetica, 11f);
            PdfStringFormat rightAlignment = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Middle);
            PdfSolidBrush brush = new PdfSolidBrush(Color.Black);
            page.Canvas.DrawString("Fecha: " + fecha, font, brush, 244, 156, rightAlignment);

            pdfDocument.SaveToFile($@"{pathDocumento}", Spire.Pdf.FileFormat.PDF);

            return $@"{pathDocumento}";
        }





        public static string CreatePortadaExcel(string path, string radicado)
        {
            // Create workbook and remove extra sheets
            Workbook workbook = new Workbook();
            workbook.Worksheets[2].Remove();
            workbook.Worksheets[1].Remove();

            Worksheet sheet = workbook.Worksheets[0];
            sheet.Name = radicado;

            // Load HTML into document
            Document doc = new Document();
            StringReader sr = new StringReader($"<span style=\"border-width:thin;border-color:#FFFFFF;\"><font color=#000000 size=8><b>{radicado}</b></font></span>");
            doc.LoadHTML(sr, XHTMLValidationType.None);

            // Process the document content
            foreach (Section section in doc.Sections)
            {
                foreach (Paragraph paragraph in section.Paragraphs)
                {
                    if (paragraph.Items.Count > 0)
                    {
                        workbook.Worksheets[0].Range["A1"].RichText.Text += paragraph.Text;
                    }

                    int index = 0;

                    foreach (var item in paragraph.Items)
                    {
                        if (item is TextRange textRange)
                        {
                            CharacterFormat format = textRange.CharacterFormat;

                            ExcelFont excelFont = workbook.CreateFont();
                            excelFont.FontName = format.FontName;
                            excelFont.Size = format.FontSize;
                            excelFont.IsBold = format.Bold;
                            excelFont.IsItalic = format.Italic;
                            excelFont.Underline = format.UnderlineStyle != UnderlineStyle.None
                                                 ? FontUnderlineType.Single
                                                 : FontUnderlineType.None;

                            excelFont.Color = format.TextColor;

                            for (int i = index; i < textRange.Text.Length + index; i++)
                            {
                                workbook.Worksheets[0].Range["A1"].RichText.SetFont(i, i, excelFont);
                            }

                            index += textRange.Text.Length;
                        }
                    }
                }
            }

            // Save the Excel file
            string outputPath = Path.Combine(path, "Portada.xlsx");
            workbook.SaveToFile(outputPath, ExcelVersion.Version2013);
            return outputPath;
        }



        public static void MergeDocumentExcel(string pathPortada, string pathDocumento)
        {

            Workbook newbook = new Workbook();
            newbook.Version = ExcelVersion.Version2013;
            newbook.Worksheets.Clear();

            Workbook tempbook = new Workbook();
            string[] excelFiles = new String[] { pathPortada, pathDocumento };

            for (int i = 0; i < excelFiles.Length; i++)
            {
                tempbook.LoadFromFile(excelFiles[i]);
                foreach (Worksheet sheetX in tempbook.Worksheets)
                {
                    newbook.Worksheets.AddCopy(sheetX);
                }
            }

            newbook.SaveToFile(pathDocumento, ExcelVersion.Version2013);

        }


    }
}

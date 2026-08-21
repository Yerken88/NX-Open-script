using System;
using System.Collections.Generic;
using NXOpen;
using NXOpen.Drawings;
using NXOpen.PDM;

public class NXJournal
{
    static Session s = Session.GetSession();
    static UI ui = UI.GetUI();

    public static void Main(string[] args)
    {
        PrintPDFBuilder b = null;
        try
        {
            Part p = s.Parts.Work;
            if (p == null) throw new Exception("No work part is open.");

            var sheets = new List<NXObject>();
            foreach (DraftingDrawingSheet x in p.DraftingDrawingSheets) sheets.Add(x);
            if (sheets.Count == 0) throw new Exception("No drawing sheets found.");

            string id = Attr(p, "DB_PART_NO"), rev = Attr(p, "DB_PART_REV");
            if (id == "" || rev == "")
            {
                string[] a = p.FullPath.Replace("@DB/", "").Trim('/').Split('/');
                if (a.Length < 2) throw new Exception("Item ID and Revision ID not found.");
                id = a[0]; rev = a[1];
            }

            string name = id + "-" + rev;
            b = p.PlotManager.CreatePrintPdfbuilder();
            b.Relation = PrintPDFBuilder.RelationOption.Specification;
            b.DatasetType = "PDF";
            b.NamedReferenceType = "PDF_Reference";
            b.DeleteDatasets = true;
            b.RasterImages = true;
            b.CustomSymbolsInForeground = false;
            b.ImageResolution = PrintPDFBuilder.ImageResolutionOption.High;
            b.ShadedGeometry = true;
            b.DatasetName = name;
            b.Assign();
            b.DatasetName = name;
            b.SourceBuilder.SetSheets(sheets.ToArray());

            bool exists = Exists(p, name);

            if (exists)
            {
                int answer = ui.NXMessageBox.Show("Export PDF (KPE)",
                    NXMessageBox.DialogType.Question,
                    name + " already exists.\n\nYes - replace.\nNo - cancel.");
                if (answer != 1) return;
                b.Action = PrintPDFBuilder.ActionOption.Overwrite;
            }
            else b.Action = PrintPDFBuilder.ActionOption.New;

            b.CreateNewFromUi = b.Action == PrintPDFBuilder.ActionOption.New;
            b.DatasetName = name;
            b.Commit();

            ui.NXMessageBox.Show("Export PDF (KPE)",
                NXMessageBox.DialogType.Information,
                "PDF " + (exists ? "replaced: " : "uploaded: ") + name +
                "\nSheets: " + sheets.Count);
        }
        catch (Exception e)
        {
            ui.NXMessageBox.Show("Export PDF (KPE)",
                NXMessageBox.DialogType.Error, e.Message);
        }
        finally { if (b != null) b.Destroy(); }
    }

    static string Attr(Part p, string name)
    {
        try { return p.GetStringUserAttribute(name, -1).Trim(); }
        catch { return ""; }
    }

    static bool Exists(Part p, string name)
    {
        FileManagement fm = s.PdmSession.NewFileManagement();
        PdmFile[] files = null;
        try
        {
            int[] count;
            fm.GetAttachedFiles(
                new NXObject[] { p },
                new string[] { "IMAN_specification" },
                new string[] { "PDF" },
                new string[] { name },
                new string[] { "PDF_Reference" },
                new string[] { "" },
                new string[] { "" },
                "", out count, out files);

            if (count == null || count.Length == 0)
                throw new Exception("Failed to check PDF in Teamcenter.");

            return count[0] > 0;
        }
        finally
        {
            if (files != null)
                foreach (PdmFile file in files)
                    if (file != null) file.Dispose();
            fm.Dispose();
        }
    }

    public static int GetUnloadOption(string dummy)
    {
        return (int)Session.LibraryUnloadOption.Immediately;
    }
}

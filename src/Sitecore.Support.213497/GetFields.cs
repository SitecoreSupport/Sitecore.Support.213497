namespace Sitecore.Support.Shell.Applications.ContentEditor.Pipelines.GetContentEditorFields
{
  using Sitecore;
  using Sitecore.Collections;
  using Sitecore.Configuration;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Data.Templates;
  using Sitecore.Diagnostics;
  using Sitecore.SecurityModel;
  using Sitecore.Shell.Applications.ContentEditor.Pipelines.GetContentEditorFields;
  using Sitecore.Shell.Applications.ContentManager;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;

  public class GetFields
  {
    protected virtual bool CanShowField(Sitecore.Data.Fields.Field field, TemplateField templateField)
    {
      Assert.ArgumentNotNull(field, "field");
      Assert.ArgumentNotNull(templateField, "templateField");
      bool flag = true;
      if (this.ShowDataFieldsOnly)
      {
        flag = ItemUtil.IsDataField(templateField);
      }
      bool flag2 = true;
      if (!this.ShowHiddenFields)
      {
        Item item = field.Database.GetItem(templateField.ID, Context.Language);
        if ((item != null) && item.Appearance.Hidden)
        {
          flag2 = false;
        }
      }
      return ((((field.Name.Length > 0) && field.CanRead) && (!this.ShowDataFieldsOnly | flag)) & flag2);
    }

    private static FieldCollection GetFieldCollection(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      FieldCollection fields = item.Fields;
      fields.ReadAll();
      fields.Sort();
      return Assert.ResultNotNull<FieldCollection>(fields);
    }
    //helper method 
    private int CompareFields(int sortorder1, int sortorder2, string fieldName1, string fieldName2)
    {
      if (sortorder1 != sortorder2)
      {
        return (sortorder1 - sortorder2);
      }
      if ((fieldName1.Length > 0) && (fieldName2.Length > 0))
      {
        if ((fieldName1[0] == '_') && (fieldName2[0] != '_'))
        {
          return 1;
        }
        if ((fieldName2[0] == '_') && (fieldName1[0] != '_'))
        {
          return -1;
        }
      }
      return fieldName1.CompareTo(fieldName2);
    }
    //helper method
    private int CompareSections(int sectionSortorder1, int sectionSortorder2, string section1, string section2)
    {
      if (sectionSortorder1 != sectionSortorder2)
      {
        return (sectionSortorder1 - sectionSortorder2);
      }
      if ((section1.Length > 0) && (section2.Length > 0))
      {
        if ((section1[0] == '_') && (section2[0] != '_'))
        {
          return 1;
        }
        if ((section2[0] == '_') && (section1[0] != '_'))
        {
          return -1;
        }
      }
      return section1.CompareTo(section2);
    }
    private void GetSections(GetContentEditorFieldsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Item item = args.Item;
      Assert.IsNotNull(item, "item");
      Editor.Sections sections = args.Sections;
      #region Modified code 
      List<Field> fields = GetFieldCollection(item).ToList();
      fields.Sort((x, y) =>
      {
        int num = CompareFields(x.Sortorder, y.Sortorder, x.Name, y.Name);
        if (num != 0)
        {
          return num;
        }
        return CompareSections(x.SectionSortorder, y.SectionSortorder, x.Section, y.Section);
      });
      #endregion
      foreach (Sitecore.Data.Fields.Field field in fields)
      {
        TemplateField templateField = field.GetTemplateField();
        if ((templateField != null) && this.CanShowField(field, templateField))
        {
          TemplateSection templateSection = templateField.Section;
          if (templateSection != null)
          {
            Editor.Field field3;
            Editor.Section section2 = sections[templateSection.Name];
            if (section2 == null)
            {
              section2 = new Editor.Section(templateSection);
              if (args.Item != null)
              {
                section2.ControlID = section2.ControlID + args.Item.ID.ToShortID().ToString();
              }
              sections.Add(section2);
              using (new SecurityDisabler())
              {
                Item item2 = args.Item.RuntimeSettings.TemplateDatabase.GetItem(templateSection.ID, Context.Language);
                if (item2 != null)
                {
                  section2.CollapsedByDefault = item2["Collapsed by Default"] == "1";
                  section2.DisplayName = item2.GetUIDisplayName();
                }
              }
            }
            if (((field.ID == FieldIDs.WorkflowState) || (field.ID == FieldIDs.Workflow)) && (!item.IsClone || !Settings.ItemCloning.InheritWorkflowData))
            {
              field3 = new Editor.Field(field, templateField, field.GetValue(false, false) ?? string.Empty);
            }
            else
            {
              field3 = new Editor.Field(field, templateField);
            }
            section2.Fields.Add(field3);
            args.AddFieldInfo(field, field3.ControlID);
          }
        }
      }
      sections.SortSections();
    }

    public void Process(GetContentEditorFieldsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (args.Item != null)
      {
        this.ShowDataFieldsOnly = args.ShowDataFieldsOnly;
        this.ShowHiddenFields = args.ShowHiddenFields;
        this.GetSections(args);
      }
    }

    public bool ShowDataFieldsOnly { get; private set; }

    public bool ShowHiddenFields { get; set; }
  }
}
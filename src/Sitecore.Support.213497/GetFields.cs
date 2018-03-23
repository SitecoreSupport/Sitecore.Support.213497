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

    private void GetSections(GetContentEditorFieldsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Item item = args.Item;
      Assert.IsNotNull(item, "item");
      Editor.Sections sections = args.Sections;
      foreach (Sitecore.Data.Fields.Field field in GetFieldCollection(item))
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
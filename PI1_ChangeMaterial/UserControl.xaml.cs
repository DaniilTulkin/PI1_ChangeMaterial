using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PI1_ChangeMaterial
{
    /// <summary>
    /// Interaction logic for UserControl.xaml
    /// </summary>
    public partial class MainUserControl : Window
    {
        #region private methods

        private UIDocument uidoc;

        private Document doc;

        private Element elementType;

        private int elementCategory;

        private HostObjAttributes hoa;

        private CompoundStructure structure;

        #endregion

        #region public members

        public ObservableCollection<MaterialInformation> matInfList { get; private set; } = new ObservableCollection<MaterialInformation>();

        #endregion

        #region constructor

        public MainUserControl(UIDocument uidoc, Reference reference)
        {

            InitializeComponent();

            this.uidoc = uidoc;
            this.doc = uidoc.Application.ActiveUIDocument.Document;

            int wallCategory = (int)BuiltInCategory.OST_Walls;
            int floorCategory = (int)BuiltInCategory.OST_Floors;
            int roofCategory = (int)BuiltInCategory.OST_Roofs;
            int railingCategory = (int)BuiltInCategory.OST_StairsRailing;
            int stairCategory = (int)BuiltInCategory.OST_Stairs;

            Element element = doc.GetElement(reference.ElementId);
            this.elementType = doc.GetElement(element.GetTypeId());
            this.elementCategory = elementType.Category.Id.IntegerValue;

            var materialsList = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .ToList();
            materialsList.Sort((x, y) => string.Compare(x.Name, y.Name));

            if (elementCategory == wallCategory ||
                elementCategory == floorCategory ||
                elementCategory == roofCategory)
            {
                this.hoa = elementType as HostObjAttributes;
                this.structure = hoa.GetCompoundStructure();
                IList<CompoundStructureLayer> layers = structure.GetLayers();
                foreach (CompoundStructureLayer layer in layers)
                {
                    MaterialInformation matInf = new MaterialInformation(
                                                              layer,
                                                              doc.GetElement(layer.MaterialId),
                                                              materialsList);

                    matInfList.Add(matInf);
                }
            }

            else if (elementCategory == railingCategory)
            {
                RailingType railingType = elementType as RailingType;
                PostPattern balusterPostPattern = railingType.BalusterPlacement.PostPattern;

                HashSet<ElementId> balusterInfoElSet = new HashSet<ElementId>();
                balusterInfoElSet.Add(balusterPostPattern.StartPost.BalusterFamilyId);
                balusterInfoElSet.Add(balusterPostPattern.CornerPost.BalusterFamilyId);
                balusterInfoElSet.Add(balusterPostPattern.EndPost.BalusterFamilyId);

                foreach (ElementId balusterInfoElId in balusterInfoElSet)
                {
                    Element balusterInfoEl = doc.GetElement(balusterInfoElId);
                    ParameterSet balusterTypeParameters = balusterInfoEl.Parameters;
                    foreach (Parameter parameter in balusterTypeParameters)
                    {
                        if (!parameter.IsReadOnly)
                        {
                            if (parameter.Definition.ParameterType == ParameterType.Material)
                            {
                                MaterialInformation matInf = new MaterialInformation(
                                                               parameter,
                                                               doc.GetElement(parameter.AsElementId()),
                                                               materialsList);

                                matInfList.Add(matInf);
                            }
                        }
                    }
                }

                NonContinuousRailStructure railStructure = railingType.RailStructure;
                int railStructureCount = railStructure.GetNonContinuousRailCount();
                for (int i = 0; i < railStructureCount; i++)
                {
                    NonContinuousRailInfo railInfo = railStructure.GetNonContinuousRail(i);
                    MaterialInformation matInf = new MaterialInformation(
                                                     railInfo,
                                                     doc.GetElement(railInfo.MaterialId),
                                                     materialsList);

                    matInfList.Add(matInf);
                }
            }

            else if (elementCategory == stairCategory)
            {
                StairsType stairsType = elementType as StairsType;

                HashSet<ElementId> stairElementsIds = new HashSet<ElementId>();
                stairElementsIds.Add(stairsType.RunType);
                stairElementsIds.Add(stairsType.LandingType);
                stairElementsIds.Add(stairsType.LeftSideSupportType);
                stairElementsIds.Add(stairsType.MiddleSupportType);
                stairElementsIds.Add(stairsType.RightSideSupportType);

                foreach (ElementId stairElementsId in stairElementsIds)
                {
                    if (stairElementsId.IntegerValue != -1)
                    {
                        Element stairElement = doc.GetElement(stairElementsId);
                        ParameterSet stairElementParameters = stairElement.Parameters;
                        foreach (Parameter parameter in stairElementParameters)
                        {
                            if (!parameter.IsReadOnly)
                            {
                                if (parameter.Definition.ParameterType == ParameterType.Material)
                                {
                                    MaterialInformation matInf = new MaterialInformation(
                                                                   parameter,
                                                                   doc.GetElement(parameter.AsElementId()),
                                                                   materialsList);

                                    matInfList.Add(matInf);
                                }
                            }
                        }
                    }
                }
            }

            else
            {
                ParameterSet typeParameters = elementType.Parameters;
                foreach (Parameter parameter in typeParameters)
                {
                    if (!parameter.IsReadOnly)
                    {
                        if (parameter.Definition.ParameterType == ParameterType.Material)
                        {
                            MaterialInformation matInf = new MaterialInformation(
                                                           parameter,
                                                           doc.GetElement(parameter.AsElementId()),
                                                           materialsList);

                            matInfList.Add(matInf);
                        }
                    }
                }

                ParameterSet instanceParameters = element.Parameters;
                foreach (Parameter parameter in instanceParameters)
                {
                    if (!parameter.IsReadOnly)
                    {
                        if (parameter.Definition.ParameterType == ParameterType.Material)
                        {
                            MaterialInformation matInf = new MaterialInformation(
                                                           parameter,
                                                           doc.GetElement(parameter.AsElementId()),
                                                           materialsList);

                            matInfList.Add(matInf);
                        }
                    }
                }
            }

            icMaterialsList.ItemsSource = matInfList;
        }

        #endregion

        #region events

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            foreach (System.Windows.Controls.ComboBox cb in FindVisualChildren<System.Windows.Controls.ComboBox>(this))
            {
                System.Windows.Controls.ComboBox cmb = cb;
                MaterialInformation dc = cmb.DataContext as MaterialInformation;
                Parameter parameter = dc.Parameter;
                CompoundStructureLayer layer = dc.CompoundStructureLayer;
                NonContinuousRailInfo railInfo = dc.NonContinuousRailInfo;
                Element material = (Element)cmb.SelectedItem;
                if (material == null)
                {
                    return;
                }

                using (Transaction t = new Transaction(doc, "Поменять материал"))
                {
                    t.Start();

                    if (layer != null)
                    {
                        structure.SetMaterialId(layer.LayerId, material.Id);
                        hoa.SetCompoundStructure(structure);
                    }

                    else if (railInfo != null)
                    {
                        railInfo.MaterialId = material.Id;
                    }

                    else
                    {
                        Element element = parameter.Element;
                        if (element is FamilyInstance)
                        {
                            Element elType = doc.GetElement(element.GetTypeId());
                            ElementClassFilter classFilter = new ElementClassFilter(typeof(FamilyInstance));
                            IList<ElementId> elementIds = elType.GetDependentElements(classFilter);
                            foreach (ElementId elementId in elementIds)
                            {
                                Parameter fPar = doc.GetElement(elementId).get_Parameter(parameter.Definition);
                                fPar.Set(material.Id);
                            }
                        }
                        else
                        {
                            parameter.Set(material.Id);
                        }
                    }

                    t.Commit();
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }

        #endregion

        #region private methods

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = VisualTreeHelper.GetChild(depObj, i);

                    if (child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }

        }

        #endregion

    }

    #region public class

    public class MaterialInformation
    {
        public Parameter Parameter { get; set; }

        public CompoundStructureLayer CompoundStructureLayer { get; set; }

        public string Name { get; set; }

        public Element Material { get; set; }

        public List<Element> MaterialsList { get; set; }

        public NonContinuousRailInfo NonContinuousRailInfo { get; set; }

        public MaterialInformation(Parameter parameter, Element material, List<Element> materialsList)
        {
            Parameter = parameter;
            Material = material;
            MaterialsList = materialsList;
            Name = parameter.Definition.Name;
        }

        public MaterialInformation(CompoundStructureLayer compoundStructureLayer, Element material, List<Element> materialsList)
        {
            CompoundStructureLayer = compoundStructureLayer;
            Material = material;
            MaterialsList = materialsList;
            Name = Convert.ToString(compoundStructureLayer.LayerId) + ". " + Convert.ToString(compoundStructureLayer.Function);
        }

        public MaterialInformation(NonContinuousRailInfo railInfo, Element material, List<Element> materialsList)
        {
            NonContinuousRailInfo = railInfo;
            Material = material;
            MaterialsList = materialsList;
            Name = railInfo.Name;
        }
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MrLuje.LazyPackagesCleaner.Model;

namespace MrLuje.LazyPackagesCleaner
{
    public partial class ReferenceConflicts : Form
    {
        public ReferenceConflicts()
        {
            InitializeComponent();
        }

        public void SetValue(IEnumerable<PackageVersions> packageVersions)
        {
            var tt = new ToolTip();

            foreach (var packageVersion in packageVersions)
            {
                var lbl = new Label();
                lbl.Text = packageVersion.PackageName;
                flowLayoutPanel1.Controls.Add(lbl);

                tt.SetToolTip(lbl, packageVersion.PackageName);

                var clbVersions = new CheckedListBox();
                clbVersions.CheckOnClick = true;
                clbVersions.ItemCheck += clbVersions_ItemCheck;
                foreach (var version in packageVersion.Versions.OrderBy(s => s))
                {
                    clbVersions.Items.Add(version);
                }
                flowLayoutPanel1.Controls.Add(clbVersions);

                btnValidate.Enabled = false;
            }
        }

        void clbVersions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var cbl = (CheckedListBox)sender;
            if (e.NewValue == CheckState.Checked && cbl.CheckedItems.Count > 0)
            {
                cbl.ItemCheck -= clbVersions_ItemCheck;
                cbl.SetItemChecked(cbl.CheckedIndices[0], false);
                cbl.ItemCheck += clbVersions_ItemCheck;
            }

            // Only enable validate btn when there is one version selected per package
            btnValidate.Enabled = flowLayoutPanel1
                                 .Controls
                                 .OfType<CheckedListBox>()
                                 .Where(cb => cb != cbl)
                                 .All(cb => cb.CheckedItems.Count == 1)
                                  && e.NewValue == CheckState.Checked;
        }


        public event Action<List<SelectedVersion>> Confirmed;
        
        private void btnValidate_Click(object sender, EventArgs e)
        {
            var name = "";
            var version = "";
            var result = new List<SelectedVersion>();
            foreach (var ctrl in flowLayoutPanel1.Controls)
            {
                if (ctrl is Label) name = ((Label)ctrl).Text;
                if (ctrl is CheckedListBox) version = ((CheckedListBox)ctrl).SelectedItem.ToString();

                if (name != "" && version != "")
                {
                    result.Add(new SelectedVersion(name, version));
                    name = version = "";
                }
            }

            if (Confirmed != null)
            {
                Confirmed(result);
            }

            this.Close();
        }
    }
}

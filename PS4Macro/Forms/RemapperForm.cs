﻿// PS4Macro (File: Forms/RemapperForm.cs)
//
// Copyright (c) 2018 Komefai
//
// Visit http://komefai.com for more information
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using PS4Macro.Classes.Remapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PS4Macro.Forms
{
    public partial class RemapperForm : Form
    {
        public Remapper m_Remapper;

        private bool m_FormLoaded;

        private BindingList<MappingAction> m_MappingsBindingList;
        private BindingList<MacroAction> m_MacrosBindingList;

        public RemapperForm(Remapper remapper)
        {
            InitializeComponent();

            m_Remapper = remapper;
        }

        private void BindMappingsDataGrid()
        {
            mappingsDataGridView.AutoGenerateColumns = false;

            m_MappingsBindingList = new BindingList<MappingAction>(m_Remapper.MappingsDataBinding);
            mappingsDataGridView.DataSource = m_MappingsBindingList;
        }

        private void BindMacrosDataGrid()
        {
            macrosDataGridView.AutoGenerateColumns = false;

            m_MacrosBindingList = new BindingList<MacroAction>(m_Remapper.MacrosDataBinding);
            macrosDataGridView.DataSource = m_MacrosBindingList;
        }

        private bool IsDuplicatedKey(DataGridView dataGridView, DataGridViewCell editingCell, Keys key, ref string duplicatedValue)
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (editingCell == cell)
                        continue;

                    if (!dataGridView.Columns[cell.ColumnIndex].HeaderText.Equals("Key"))
                        continue;

                    if (cell.Value == null)
                        continue;

                    Keys cellKey = (Keys)cell.Value;
                    if (cellKey != Keys.None && cellKey == key)
                    {
                        if (dataGridView == mappingsDataGridView)
                        {
                            duplicatedValue = row.Cells["Button"].Value.ToString();
                            return true;
                        }
                        else if (dataGridView == macrosDataGridView)
                        {
                            duplicatedValue = row.Cells["_Name"].Value.ToString();
                            return true;
                        }
                    }
                    break;
                }
            }

            return false;
        }

        private bool IsDuplicatedKey(DataGridViewCell editingCell, Keys key, ref string duplicatedValue)
        {
            return IsDuplicatedKey(mappingsDataGridView, editingCell, key, ref duplicatedValue) || 
                IsDuplicatedKey(macrosDataGridView, editingCell, key, ref duplicatedValue);
        }

        private bool PredictCorrectKey(string input, ref Keys result)
        {
            Keys parsedKey;
            
            // Need correction
            if (!Enum.TryParse(input, out parsedKey))
            {
                // Predict upper case
                if (Enum.TryParse(input.ToUpper(), out parsedKey))
                {
                    result = parsedKey;
                    return true;
                }
            }
            // No need to correct
            else
            {
                result = parsedKey;
                return true;
            }

            return false;
        }

        private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!m_FormLoaded) return;
            m_Remapper.CreateActions();
        }

        private void OnCellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            var dataGridView = sender as DataGridView;
            var column = dataGridView.Columns[e.ColumnIndex];

            // Ignore if not editing
            if (!dataGridView.IsCurrentCellInEditMode)
                return;

            // Filter for Key column
            if (column.HeaderText.Equals("Key"))
            {
                var row = dataGridView.Rows[e.RowIndex];
                var cell = row.Cells[e.ColumnIndex];
                var value = e.FormattedValue.ToString();

                // Try to correct the key
                Keys parsedKey = Keys.None;
                if (PredictCorrectKey(value, ref parsedKey))
                {
                    // Check if duplicated
                    string duplicatedValue = null;
                    if (IsDuplicatedKey(cell, parsedKey, ref duplicatedValue))
                    {
                        MessageBox.Show($"Key \"{value}\" already mapped to \"{duplicatedValue}\"", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.Cancel = true;
                    }
                }

            }
        }

        private void OnDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            var dataGridView = sender as DataGridView;
            var column = dataGridView.Columns[e.ColumnIndex];

            // Filter for Key column
            if (column.HeaderText.Equals("Key"))
            {
                var row = dataGridView.Rows[e.RowIndex];
                var cell = row.Cells[e.ColumnIndex];
                var value = dataGridView.EditingControl.Text;

                Action doneEditing = () =>
                {
                    // Restore normal behavior
                    dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    dataGridView.EndEdit();

                    if (e.RowIndex < dataGridView.Rows.Count)
                        dataGridView.CurrentCell = dataGridView.Rows[e.RowIndex + 1].Cells[e.ColumnIndex];
                };

                // Default empty value to None
                if (string.IsNullOrEmpty(value))
                {
                    dataGridView.EditingControl.Text = Keys.None.ToString();
                    doneEditing();
                }
                else
                {
                    // Try to correct the key
                    Keys parsedKey = Keys.None;
                    if (PredictCorrectKey(value, ref parsedKey))
                    {
                        dataGridView.EditingControl.Text = parsedKey.ToString();
                        doneEditing();
                    }
                    else
                    {
                        MessageBox.Show($"Key \"{value}\" does not exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RemapperForm_Load(object sender, EventArgs e)
        {
            // Bind data to UI
            BindMappingsDataGrid();
            BindMacrosDataGrid();

            // Mark form as loaded
            m_FormLoaded = true;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            m_Remapper.SaveBindings();
        }

        private void mappingsDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            OnCellValueChanged(sender, e);
        }

        private void macrosDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            OnCellValueChanged(sender, e);
        }

        private void mappingsDataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            OnCellValidating(sender, e);
        }

        private void macrosDataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            OnCellValidating(sender, e);
        }

        private void mappingsDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            OnDataError(sender, e);
        }

        private void macrosDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            OnDataError(sender, e);
        }

        private void mappingsDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //var dataGridView = sender as DataGridView;
            //if (dataGridView.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            //{

            //}
        }

        private void macrosDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var dataGridView = sender as DataGridView;
            var rowIndex = e.RowIndex;

            // Browse macro
            if (dataGridView.Columns[e.ColumnIndex] is DataGridViewButtonColumn && rowIndex >= 0)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.InitialDirectory = Application.StartupPath;
                openFileDialog.Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 0;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string path = openFileDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);

                    if (rowIndex >= m_Remapper.MacrosDataBinding.Count)
                    {
                        m_Remapper.MacrosDataBinding.Add(new MacroAction());
                    }

                    var item = m_Remapper.MacrosDataBinding[rowIndex];
                    item.Name = fileName;
                    //item.Key = Keys.None;
                    item.Path = path;

                    // Reset bindings
                    m_MacrosBindingList.ResetBindings();
                }
            }
        }
    }
}
namespace DnD_character_list
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            SpesiesButton = new Button();
            BackgroundButton = new Button();
            ClassesButton = new Button();
            TraitsButton = new Button();
            SpellsButton = new Button();
            InvetoryButton = new Button();
            label1 = new Label();
            button1 = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.Untitled;
            pictureBox1.Location = new Point(13, 13);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(81, 81);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // SpesiesButton
            // 
            SpesiesButton.Location = new Point(13, 113);
            SpesiesButton.Name = "SpesiesButton";
            SpesiesButton.Size = new Size(199, 70);
            SpesiesButton.TabIndex = 1;
            SpesiesButton.Text = "Обновить расы";
            SpesiesButton.UseVisualStyleBackColor = true;
            SpesiesButton.Click += SpesiesButton_Click;
            // 
            // BackgroundButton
            // 
            BackgroundButton.Location = new Point(218, 113);
            BackgroundButton.Name = "BackgroundButton";
            BackgroundButton.Size = new Size(199, 70);
            BackgroundButton.TabIndex = 2;
            BackgroundButton.Text = "Обновить предистории";
            BackgroundButton.UseVisualStyleBackColor = true;
            BackgroundButton.Click += BackgroundButton_Click;
            // 
            // ClassesButton
            // 
            ClassesButton.Location = new Point(423, 113);
            ClassesButton.Name = "ClassesButton";
            ClassesButton.Size = new Size(199, 70);
            ClassesButton.TabIndex = 3;
            ClassesButton.Text = "Обновить Классы";
            ClassesButton.UseVisualStyleBackColor = true;
            ClassesButton.Click += ClassesButton_Click;
            // 
            // TraitsButton
            // 
            TraitsButton.Location = new Point(13, 189);
            TraitsButton.Name = "TraitsButton";
            TraitsButton.Size = new Size(199, 70);
            TraitsButton.TabIndex = 4;
            TraitsButton.Text = "Обновить черты";
            TraitsButton.UseVisualStyleBackColor = true;
            TraitsButton.Click += TraitsButton_Click;
            // 
            // SpellsButton
            // 
            SpellsButton.Location = new Point(218, 189);
            SpellsButton.Name = "SpellsButton";
            SpellsButton.Size = new Size(199, 70);
            SpellsButton.TabIndex = 5;
            SpellsButton.Text = "Обновить заклинания";
            SpellsButton.UseVisualStyleBackColor = true;
            SpellsButton.Click += SpellsButton_Click;
            // 
            // InvetoryButton
            // 
            InvetoryButton.Location = new Point(423, 189);
            InvetoryButton.Name = "InvetoryButton";
            InvetoryButton.Size = new Size(199, 70);
            InvetoryButton.TabIndex = 6;
            InvetoryButton.Text = "Обновить сокровища";
            InvetoryButton.UseVisualStyleBackColor = true;
            InvetoryButton.Click += InvetoryButton_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(62, 279);
            label1.Name = "label1";
            label1.Size = new Size(499, 20);
            label1.TabIndex = 7;
            label1.Text = "⚠️ВНИМАНИЕ! Обновление с ttg.club занимает от 5 минут до 3часов!";
            // 
            // button1
            // 
            button1.Location = new Point(491, 13);
            button1.Name = "button1";
            button1.Size = new Size(131, 57);
            button1.TabIndex = 8;
            button1.Text = "Обновить всё";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button1);
            Controls.Add(label1);
            Controls.Add(InvetoryButton);
            Controls.Add(SpellsButton);
            Controls.Add(TraitsButton);
            Controls.Add(ClassesButton);
            Controls.Add(BackgroundButton);
            Controls.Add(SpesiesButton);
            Controls.Add(pictureBox1);
            Name = "Form2";
            Text = "DataBase";
            FormClosing += Form2_FormClosing;
            FormClosed += Form2_FormClosed;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }


        #endregion

        private PictureBox pictureBox1;
        private Button SpesiesButton;
        private Button BackgroundButton;
        private Button ClassesButton;
        private Button TraitsButton;
        private Button SpellsButton;
        private Button InvetoryButton;
        private Label label1;
        private Button button1;
    }
}
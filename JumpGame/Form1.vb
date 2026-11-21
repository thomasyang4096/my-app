Imports System.Diagnostics
Imports System.Drawing.Drawing2D

Public Class Form1
    Private Const MaximumChargeMilliseconds As Double = 1500
    Private Const JumpDurationMilliseconds As Double = 850
    Private Const MovementStepPixels As Single = 12.0F

    Private ReadOnly scene As New JumpScene()
    Private ReadOnly chargeBar As New ProgressBar()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly jumpButton As New Button()
    Private ReadOnly animationTimer As New Timer()

    Private isCharging As Boolean
    Private isJumping As Boolean
    Private isKeyboardCharging As Boolean
    Private isLeftHeld As Boolean
    Private isRightHeld As Boolean
    Private chargeStartedAt As Long
    Private jumpStartedAt As Long

    Public Sub New()
        InitializeComponent()
        components.Add(animationTimer)
        ConfigureWindow()
    End Sub

    Private Sub ConfigureWindow()
        Text = "蓄力跳跳"
        ClientSize = New Size(840, 560)
        MinimumSize = New Size(640, 470)
        StartPosition = FormStartPosition.CenterScreen
        BackColor = Color.FromArgb(245, 248, 252)
        KeyPreview = True

        Dim layout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(20),
            .ColumnCount = 1,
            .RowCount = 2
        }
        layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 100.0F))

        scene.Dock = DockStyle.Fill
        scene.Margin = New Padding(0, 0, 0, 14)

        Dim controlsLayout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 2
        }
        controlsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 200.0F))
        controlsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        controlsLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0F))
        controlsLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0F))

        jumpButton.Dock = DockStyle.Fill
        jumpButton.Margin = New Padding(0, 0, 16, 0)
        jumpButton.Text = "按住蓄力"
        jumpButton.Font = New Font(Font.FontFamily, 12.0F, FontStyle.Bold)

        statusLabel.Dock = DockStyle.Fill
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.Text = "按住空白鍵蓄力，放開時按方向鍵斜跳；按鈕起跳為垂直跳。"
        statusLabel.AutoEllipsis = True

        chargeBar.Dock = DockStyle.Fill
        chargeBar.Margin = New Padding(0, 4, 0, 8)
        chargeBar.Minimum = 0
        chargeBar.Maximum = 100
        chargeBar.Style = ProgressBarStyle.Continuous

        controlsLayout.Controls.Add(jumpButton, 0, 0)
        controlsLayout.SetRowSpan(jumpButton, 2)
        controlsLayout.Controls.Add(statusLabel, 1, 0)
        controlsLayout.Controls.Add(chargeBar, 1, 1)

        layout.Controls.Add(scene, 0, 0)
        layout.Controls.Add(controlsLayout, 0, 1)
        Controls.Add(layout)

        animationTimer.Interval = 16
        AddHandler animationTimer.Tick, AddressOf AnimationTimerTick
        AddHandler jumpButton.MouseDown, AddressOf JumpButtonMouseDown
        AddHandler jumpButton.MouseUp, AddressOf JumpButtonMouseUp
        AddHandler KeyDown, AddressOf FormKeyDown
        AddHandler KeyUp, AddressOf FormKeyUp
        AddHandler Deactivate, AddressOf FormDeactivate
    End Sub

    Private Sub JumpButtonMouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            BeginCharge(False)
        End If
    End Sub

    Private Sub JumpButtonMouseUp(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            BeginJump(0)
        End If
    End Sub

    Protected Overrides Function ProcessDialogKey(keyData As Keys) As Boolean
        Dim keyCode = keyData And Keys.KeyCode
        If keyCode = Keys.Left Then
            isLeftHeld = True
            If Not isJumping AndAlso Not isCharging Then
                scene.MoveCharacter(-1, MovementStepPixels)
            End If
            Return True
        ElseIf keyCode = Keys.Right Then
            isRightHeld = True
            If Not isJumping AndAlso Not isCharging Then
                scene.MoveCharacter(1, MovementStepPixels)
            End If
            Return True
        End If

        Return MyBase.ProcessDialogKey(keyData)
    End Function

    Private Sub FormKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Space Then
            BeginCharge(True)
            e.Handled = True
        End If
    End Sub

    Private Sub FormKeyUp(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Left Then
            isLeftHeld = False
            e.Handled = True
        ElseIf e.KeyCode = Keys.Right Then
            isRightHeld = False
            e.Handled = True
        ElseIf e.KeyCode = Keys.Space Then
            BeginJump(If(isKeyboardCharging, GetHeldJumpDirection(), 0))
            e.Handled = True
        End If
    End Sub

    Private Sub FormDeactivate(sender As Object, e As EventArgs)
        If isCharging Then
            BeginJump(If(isKeyboardCharging, GetHeldJumpDirection(), 0))
        End If
        isLeftHeld = False
        isRightHeld = False
    End Sub

    Private Sub BeginCharge(allowDirectionalJump As Boolean)
        If isCharging OrElse isJumping OrElse Not jumpButton.Enabled Then
            Return
        End If

        isCharging = True
        isKeyboardCharging = allowDirectionalJump
        chargeStartedAt = Stopwatch.GetTimestamp()
        chargeBar.Value = 0
        jumpButton.Text = "蓄力中…放開起跳"
        statusLabel.Text = If(isKeyboardCharging, "蓄力中：方向鍵選方向，放開空白鍵起跳。", "蓄力中：按得越久，跳得越高。")
        animationTimer.Start()
    End Sub

    Private Sub BeginJump(horizontalDirection As Integer)
        If Not isCharging Then
            Return
        End If

        Dim chargeRatio = CSng(Math.Min(1.0, GetElapsedMilliseconds(chargeStartedAt) / MaximumChargeMilliseconds))
        isCharging = False
        isKeyboardCharging = False
        isJumping = True
        jumpStartedAt = Stopwatch.GetTimestamp()
        chargeBar.Value = CInt(Math.Round(chargeRatio * 100.0F))
        scene.StartJump(chargeRatio, horizontalDirection)
        jumpButton.Enabled = False
        jumpButton.Text = "跳躍中…"
        statusLabel.Text = $"蓄力 {chargeBar.Value}%！"
        animationTimer.Start()
    End Sub

    Private Function GetHeldJumpDirection() As Integer
        If isLeftHeld = isRightHeld Then
            Return 0
        End If

        Return If(isLeftHeld, -1, 1)
    End Function

    Private Sub AnimationTimerTick(sender As Object, e As EventArgs)
        If isCharging Then
            Dim chargeRatio = CSng(Math.Min(1.0, GetElapsedMilliseconds(chargeStartedAt) / MaximumChargeMilliseconds))
            chargeBar.Value = CInt(Math.Round(chargeRatio * 100.0F))
            statusLabel.Text = $"蓄力中：{chargeBar.Value}%（放開起跳）"
            Return
        End If

        If Not isJumping Then
            animationTimer.Stop()
            Return
        End If

        Dim jumpProgress = CSng(GetElapsedMilliseconds(jumpStartedAt) / JumpDurationMilliseconds)
        If jumpProgress >= 1.0F Then
            isJumping = False
            scene.EndJump()
            jumpButton.Enabled = True
            jumpButton.Text = "按住蓄力"
            statusLabel.Text = "跳完了！方向鍵可移動；按住空白鍵並按方向鍵斜跳。"
            animationTimer.Stop()
            Return
        End If

        scene.SetJumpProgress(jumpProgress)
    End Sub

    Private Shared Function GetElapsedMilliseconds(startedAt As Long) As Double
        Return (Stopwatch.GetTimestamp() - startedAt) * 1000.0 / Stopwatch.Frequency
    End Function
End Class

Friend Class JumpScene
    Inherits Panel

    Private Const MaximumHorizontalJumpDistance As Single = 100.0F

    Private jumpProgress As Single
    Private jumpHeight As Single
    Private characterX As Single = -1.0F
    Private jumpStartX As Single
    Private jumpHorizontalDistance As Single

    Public Sub New()
        DoubleBuffered = True
        ResizeRedraw = True
        BackColor = Color.FromArgb(190, 229, 250)
    End Sub

    Public Sub StartJump(chargeRatio As Single, horizontalDirection As Integer)
        Dim availableHeight = Math.Max(30.0F, Math.Min(190.0F, CSng(ClientSize.Height) - 165.0F))
        jumpHeight = 30.0F + Math.Max(0.0F, Math.Min(1.0F, chargeRatio)) * (availableHeight - 30.0F)
        If characterX < 0.0F Then
            characterX = CSng(Width) / 2.0F
        End If

        Dim minimumX = 30.0F
        Dim maximumX = Math.Max(minimumX, CSng(Width) - 30.0F)
        characterX = Math.Max(minimumX, Math.Min(maximumX, characterX))
        jumpStartX = characterX
        If horizontalDirection < 0 Then
            jumpHorizontalDistance = -Math.Min(MaximumHorizontalJumpDistance, jumpStartX - minimumX)
        ElseIf horizontalDirection > 0 Then
            jumpHorizontalDistance = Math.Min(MaximumHorizontalJumpDistance, maximumX - jumpStartX)
        Else
            jumpHorizontalDistance = 0.0F
        End If

        jumpProgress = 0.0F
        Invalidate()
    End Sub

    Public Sub SetJumpProgress(progress As Single)
        jumpProgress = Math.Max(0.0F, Math.Min(1.0F, progress))
        characterX = jumpStartX + jumpHorizontalDistance * jumpProgress
        Invalidate()
    End Sub

    Public Sub EndJump()
        characterX = jumpStartX + jumpHorizontalDistance
        jumpProgress = 0.0F
        jumpHeight = 0.0F
        jumpHorizontalDistance = 0.0F
        Invalidate()
    End Sub

    Public Sub MoveCharacter(direction As Integer, distance As Single)
        If characterX < 0.0F Then
            characterX = CSng(Width) / 2.0F
        End If

        Dim minimumX = 30.0F
        Dim maximumX = Math.Max(minimumX, CSng(Width) - 30.0F)
        characterX = Math.Max(minimumX, Math.Min(maximumX, characterX + direction * distance))
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Dim graphics = e.Graphics
        graphics.SmoothingMode = SmoothingMode.AntiAlias
        graphics.Clear(BackColor)

        Using cloudBrush As New SolidBrush(Color.FromArgb(235, Color.White))
            graphics.FillEllipse(cloudBrush, 70, 55, 74, 28)
            graphics.FillEllipse(cloudBrush, 92, 40, 48, 42)
            graphics.FillEllipse(cloudBrush, Math.Max(180, Width - 220), 92, 84, 30)
            graphics.FillEllipse(cloudBrush, Math.Max(200, Width - 190), 76, 48, 44)
        End Using

        Dim floorY = Height - 48
        Using groundBrush As New SolidBrush(Color.FromArgb(111, 190, 112))
            graphics.FillRectangle(groundBrush, 0, floorY, Width, Height - floorY)
        End Using
        Using groundPen As New Pen(Color.FromArgb(76, 156, 83), 3.0F)
            graphics.DrawLine(groundPen, 0, floorY, Width, floorY)
        End Using

        Dim bounce = CSng(Math.Sin(Math.PI * jumpProgress)) * jumpHeight
        If characterX < 0.0F Then
            characterX = CSng(Width) / 2.0F
        End If

        Dim maximumX = Math.Max(30.0F, CSng(Width) - 30.0F)
        characterX = Math.Max(30.0F, Math.Min(maximumX, characterX))
        Dim centerX = CInt(characterX)
        Dim feetY = CSng(floorY) - bounce
        Dim headTop = feetY - 103.0F

        Using outlinePen As New Pen(Color.FromArgb(51, 67, 87), 6.0F),
              faceBrush As New SolidBrush(Color.FromArgb(255, 202, 130)),
              shoeBrush As New SolidBrush(Color.FromArgb(75, 120, 213)),
              eyeBrush As New SolidBrush(Color.FromArgb(51, 67, 87))
            graphics.FillEllipse(faceBrush, centerX - 18, headTop, 36, 36)
            graphics.DrawEllipse(outlinePen, centerX - 18, headTop, 36, 36)
            graphics.FillEllipse(eyeBrush, centerX - 8, headTop + 13, 3, 4)
            graphics.FillEllipse(eyeBrush, centerX + 5, headTop + 13, 3, 4)
            graphics.DrawArc(outlinePen, centerX - 6, headTop + 15, 13, 11, 20, 140)
            graphics.DrawLine(outlinePen, centerX, feetY - 67, centerX, feetY - 33)

            If bounce > 8.0F Then
                graphics.DrawLine(outlinePen, centerX, feetY - 60, centerX - 25, feetY - 82)
                graphics.DrawLine(outlinePen, centerX, feetY - 60, centerX + 25, feetY - 82)
            Else
                graphics.DrawLine(outlinePen, centerX, feetY - 60, centerX - 27, feetY - 43)
                graphics.DrawLine(outlinePen, centerX, feetY - 60, centerX + 27, feetY - 43)
            End If

            graphics.DrawLine(outlinePen, centerX, feetY - 33, centerX - 15, feetY - 4)
            graphics.DrawLine(outlinePen, centerX, feetY - 33, centerX + 15, feetY - 4)
            graphics.FillEllipse(shoeBrush, centerX - 24, feetY - 8, 19, 10)
            graphics.FillEllipse(shoeBrush, centerX + 5, feetY - 8, 19, 10)
        End Using
    End Sub
End Class

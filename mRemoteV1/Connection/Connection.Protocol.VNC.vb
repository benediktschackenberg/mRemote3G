Imports mRemoteNG.App.Runtime
Imports System.ComponentModel
Imports mRemoteNG.Tools.LocalizedAttributes

Namespace Connection
    Namespace Protocol
        Public Class VNC
            Inherits Connection.Protocol.Base

#Region "Properties"
            Public Property SmartSize() As Boolean
                Get
                    Return VNC.Scaled
                End Get
                Set(ByVal value As Boolean)
                    VNC.Scaled = value
                End Set
            End Property

            Public Property ViewOnly() As Boolean
                Get
                    Return VNC.ViewOnly
                End Get
                Set(ByVal value As Boolean)
                    VNC.ViewOnly = value
                End Set
            End Property
#End Region

#Region "Private Declarations"
            Private VNC As VncSharp.RemoteDesktop
            Private Info As Connection.Info
#End Region

#Region "Public Methods"
            Public Sub New()
                Me.Control = New VncSharp.RemoteDesktop
            End Sub

            Public Overrides Function SetProps() As Boolean
                MyBase.SetProps()

                Try
                    VNC = Me.Control

                    Info = Me.InterfaceControl.Info

                    VNC.VncPort = Me.Info.Port

                    'If Info.VNCCompression <> Compression.CompNone Then
                    '    VNC.JPEGCompression = True
                    '    VNC.JPEGCompressionLevel = Info.VNCCompression
                    'End If

                    'Select Case Info.VNCEncoding
                    '    Case Encoding.EncCorre
                    '        VNC.Encoding = ViewerX.VNCEncoding.RFB_CORRE
                    '    Case Encoding.EncHextile
                    '        VNC.Encoding = ViewerX.VNCEncoding.RFB_HEXTILE
                    '    Case Encoding.EncRaw
                    '        VNC.Encoding = ViewerX.VNCEncoding.RFB_RAW
                    '    Case Encoding.EncRRE
                    '        VNC.Encoding = ViewerX.VNCEncoding.RFB_RRE
                    '    Case Encoding.EncTight
                    '        VNC.Encoding = ViewerX.VNCEncoding.RFB_TIGHT
                    '    Case Encoding.EncZlib
                    '        VNC.Encoding = ViewerX.VNCEncoding.RFB_ZLIB
                    '    Case Encoding.EncZLibHex
                    '        VNC.Encoding = ViewerX.VNCEncoding.RFB_ZLIBHEX
                    '    Case Encoding.EncZRLE
                    '        VNC.Encoding = ViewerX.VNCEncoding.RFB_ZRLE
                    'End Select

                    'If Info.VNCAuthMode = AuthMode.AuthWin Then
                    '    VNC.LoginType = ViewerX.ViewerLoginType.VLT_MSWIN
                    '    VNC.MsUser = Me.Info.Username
                    '    VNC.MsDomain = Me.Info.Domain
                    '    VNC.MsPassword = Me.Info.Password
                    'Else
                    '    VNC.LoginType = ViewerX.ViewerLoginType.VLT_VNC
                    '    VNC.Password = Me.Info.Password
                    'End If

                    'Select Case Info.VNCProxyType
                    '    Case ProxyType.ProxyNone
                    '        VNC.ProxyType = ViewerX.ConnectionProxyType.VPT_NONE
                    '    Case ProxyType.ProxyHTTP
                    '        VNC.ProxyType = ViewerX.ConnectionProxyType.VPT_HTTP
                    '    Case ProxyType.ProxySocks5
                    '        VNC.ProxyType = ViewerX.ConnectionProxyType.VPT_SOCKS5
                    '    Case ProxyType.ProxyUltra
                    '        VNC.ProxyType = ViewerX.ConnectionProxyType.VPT_ULTRA_REPEATER
                    'End Select

                    'If Info.VNCProxyType <> ProxyType.ProxyNone Then
                    '    VNC.ProxyIP = Info.VNCProxyIP
                    '    VNC.ProxyPort = Info.VNCProxyPort
                    '    VNC.ProxyUser = Info.VNCProxyUsername
                    '    VNC.ProxyPassword = Info.VNCProxyPassword
                    'End If

                    'If Info.VNCColors = Colors.Col8Bit Then
                    '    VNC.RestrictPixel = True
                    'Else
                    '    VNC.RestrictPixel = False
                    'End If

                    'VNC.ConnectingText = My.Language.strInheritConnecting & " (SmartCode VNC viewer)"
                    'VNC.DisconnectedText = My.Language.strInheritDisconnected
                    'VNC.MessageBoxes = False
                    'VNC.EndInit()

                    Return True
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncSetPropsFailed & vbNewLine & ex.ToString(), True)
                    Return False
                End Try
            End Function

            Public Overrides Function Connect() As Boolean
                Me.SetEventHandlers()

                Try
                    VNC.Connect(Me.Info.Hostname, Me.Info.VNCViewOnly, Info.VNCSmartSizeMode <> SmartSizeMode.SmartSNo)
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncConnectionOpenFailed & vbNewLine & ex.ToString())
                    Return False
                End Try

                Return True
            End Function

            Public Overrides Sub Disconnect()
                Try
                    VNC.Disconnect()
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncConnectionDisconnectFailed & vbNewLine & ex.ToString(), True)
                End Try
            End Sub

            Public Sub SendSpecialKeys(ByVal Keys As SpecialKeys)
                Try
                    Select Case Keys
                        Case SpecialKeys.CtrlAltDel
                            VNC.SendSpecialKeys(SpecialKeys.CtrlAltDel)
                        Case SpecialKeys.CtrlEsc
                            VNC.SendSpecialKeys(SpecialKeys.CtrlEsc)
                    End Select
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncSendSpecialKeysFailed & vbNewLine & ex.ToString(), True)
                End Try
            End Sub

            Public Sub ToggleSmartSize()
                Try
                    SmartSize = Not SmartSize
                    RefreshScreen()
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncToggleSmartSizeFailed & vbNewLine & ex.ToString(), True)
                End Try
            End Sub

            Public Sub ToggleViewOnly()
                Try
                    ViewOnly = Not ViewOnly
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncToggleViewOnlyFailed & vbNewLine & ex.ToString(), True)
                End Try
            End Sub


            Public Sub StartChat()
                Try
                    'If VNC.Capabilities.Chat = True Then
                    '    VNC.OpenChat()
                    'Else
                    '    mC.AddMessage(Messages.MessageClass.InformationMsg, "VNC Server doesn't support chat")
                    'End If
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncStartChatFailed & vbNewLine & ex.ToString(), True)
                End Try
            End Sub

            Public Sub StartFileTransfer()
                Try
                    'If VNC.Capabilities.FileTransfer = True Then
                    '    VNC.OpenFileTransfer()
                    'Else
                    '    mC.AddMessage(Messages.MessageClass.InformationMsg, "VNC Server doesn't support file transfers")
                    'End If
                Catch ex As Exception

                End Try
            End Sub

            Public Sub RefreshScreen()
                Try
                    VNC.FullScreenUpdate()
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncRefreshFailed & vbNewLine & ex.ToString(), True)
                End Try
            End Sub
#End Region

#Region "Private Methods"
            Private Sub SetEventHandlers()
                Try
                    AddHandler VNC.ConnectComplete, AddressOf VNCEvent_Connected
                    AddHandler VNC.ConnectionLost, AddressOf VNCEvent_Disconnected
                    AddHandler mRemoteNG.frmMain.clipboardchange, AddressOf VNCEvent_ClipboardChanged
                    If Not ((Force And Info.Force.NoCredentials) = Info.Force.NoCredentials) And Not String.IsNullOrEmpty(Info.Password) Then
                        VNC.GetPassword = AddressOf VNCEvent_Authenticate
                    End If
                Catch ex As Exception
                    MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg, My.Language.strVncSetEventHandlersFailed & vbNewLine & ex.ToString(), True)
                End Try
            End Sub
#End Region

#Region "Private Events & Handlers"
            Private Sub VNCEvent_Connected(ByVal sender As Object, ByVal e As EventArgs)
                MyBase.Event_Connected(Me)
                VNC.AutoScroll = Info.VNCSmartSizeMode = SmartSizeMode.SmartSNo
            End Sub

            Private Sub VNCEvent_Disconnected(ByVal sender As Object, ByVal e As EventArgs)
                MyBase.Event_Disconnected(sender, e.ToString)
                MyBase.Close()
            End Sub

            Private Sub VNCEvent_ClipboardChanged()
                Me.VNC.FillServerClipboard()
            End Sub

            Private Function VNCEvent_Authenticate() As String
                Return Info.Password
            End Function
#End Region

#Region "Enums"
            Public Enum Defaults
                None = 0
                Port = 5900
            End Enum

            Public Enum SpecialKeys
                CtrlAltDel
                CtrlEsc
            End Enum

            Public Enum Compression
                <LocalizedDescription("strNoCompression")>
                CompNone = 99
                <Description("0")>
                Comp0 = 0
                <Description("1")>
                Comp1 = 1
                <Description("2")>
                Comp2 = 2
                <Description("3")>
                Comp3 = 3
                <Description("4")>
                Comp4 = 4
                <Description("5")>
                Comp5 = 5
                <Description("6")>
                Comp6 = 6
                <Description("7")>
                Comp7 = 7
                <Description("8")>
                Comp8 = 8
                <Description("9")>
                Comp9 = 9
            End Enum

            Public Enum Encoding
                <Description("Raw")>
                EncRaw
                <Description("RRE")>
                EncRRE
                <Description("CoRRE")>
                EncCorre
                <Description("Hextile")>
                EncHextile
                <Description("Zlib")>
                EncZlib
                <Description("Tight")>
                EncTight
                <Description("ZlibHex")>
                EncZLibHex
                <Description("ZRLE")>
                EncZRLE
            End Enum

            Public Enum AuthMode
                <LocalizedDescription("VNC")>
                AuthVNC
                <LocalizedDescription("Windows")>
                AuthWin
            End Enum

            Public Enum ProxyType
                <LocalizedDescription("strNone")>
                ProxyNone
                <LocalizedDescription("strHttp")>
                ProxyHTTP
                <LocalizedDescription("strSocks5")>
                ProxySocks5
                <LocalizedDescription("strUltraVncRepeater")>
                ProxyUltra
            End Enum

            Public Enum Colors
                <LocalizedDescription("strNormal")>
                ColNormal
                <Description("8-bit")>
                Col8Bit
            End Enum

            Public Enum SmartSizeMode
                <LocalizedDescription("strNoSmartSize")>
                SmartSNo
                <LocalizedDescription("strFree")>
                SmartSFree
                <LocalizedDescription("strAspect")>
                SmartSAspect
            End Enum
#End Region
        End Class
    End Namespace
End Namespace
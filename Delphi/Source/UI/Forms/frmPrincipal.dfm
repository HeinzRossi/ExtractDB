object Form1: TForm1
  Left = 0
  Top = 0
  Caption = 'ExtractDB'
  ClientHeight = 720
  ClientWidth = 1120
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -12
  Font.Name = 'Segoe UI'
  Font.Style = []
  WindowState = wsMaximized
  OnCreate = FormCreate
  TextHeight = 15
  object pnlBottomBar: TPanel
    Left = 0
    Top = 673
    Width = 1120
    Height = 47
    Align = alBottom
    BevelOuter = bvNone
    Color = clWhite
    ParentBackground = False
    ShowCaption = False
    TabOrder = 0
    DesignSize = (
      1120
      47)
    object lblStatusMessage: TLabel
      Left = 18
      Top = 8
      Width = 3
      Height = 15
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -12
      Font.Name = 'Segoe UI'
      Font.Style = [fsBold]
      ParentFont = False
    end
    object lblProgressMessage: TLabel
      Left = 18
      Top = 26
      Width = 3
      Height = 15
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clGrayText
      Font.Height = -12
      Font.Name = 'Segoe UI'
      Font.Style = []
      ParentFont = False
    end
    object ProgressBarBusy: TProgressBar
      Left = 600
      Top = 17
      Width = 390
      Height = 14
      Anchors = [akTop, akRight]
      Style = pbstMarquee
      MarqueeInterval = 40
      TabOrder = 0
      Visible = False
    end
    object btnCancelar: TButton
      Left = 1016
      Top = 8
      Width = 90
      Height = 30
      Anchors = [akTop, akRight]
      Caption = 'Cancelar'
      TabOrder = 1
    end
  end
  object pnlSidebar: TPanel
    Left = 0
    Top = 0
    Width = 246
    Height = 673
    Align = alLeft
    BevelOuter = bvNone
    Color = clWhite
    ParentBackground = False
    ShowCaption = False
    TabOrder = 1
    object lblAppTitle: TLabel
      AlignWithMargins = True
      Left = 18
      Top = 3
      Width = 81
      Height = 25
      Margins.Left = 18
      Align = alTop
      Caption = 'ExtractDB'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -19
      Font.Name = 'Segoe UI'
      Font.Style = []
      ParentFont = False
    end
    object lblCurrentStepTitle: TLabel
      AlignWithMargins = True
      Left = 18
      Top = 34
      Width = 46
      Height = 15
      Margins.Left = 18
      Align = alTop
      Caption = 'Conex'#227'o'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clGrayText
      Font.Height = -12
      Font.Name = 'Segoe UI'
      Font.Style = []
      ParentFont = False
    end
    object btnStepConexao: TButton
      AlignWithMargins = True
      Left = 18
      Top = 55
      Width = 210
      Height = 33
      Margins.Left = 18
      Margins.Right = 18
      Align = alTop
      Caption = '1  Conex'#227'o'
      TabOrder = 0
    end
    object btnStepObjetos: TButton
      AlignWithMargins = True
      Left = 18
      Top = 94
      Width = 210
      Height = 33
      Margins.Left = 18
      Margins.Right = 18
      Align = alTop
      Caption = '2  Objetos'
      TabOrder = 1
    end
    object btnStepSaida: TButton
      AlignWithMargins = True
      Left = 18
      Top = 133
      Width = 210
      Height = 33
      Margins.Left = 18
      Margins.Right = 18
      Align = alTop
      Caption = '3  Sa'#237'da'
      TabOrder = 2
    end
    object pnlHint: TPanel
      AlignWithMargins = True
      Left = 18
      Top = 172
      Width = 210
      Height = 498
      Margins.Left = 18
      Margins.Right = 18
      Align = alClient
      BevelOuter = bvNone
      Color = clSkyBlue
      ParentBackground = False
      TabOrder = 3
      object lblHint: TLabel
        Left = 12
        Top = 10
        Width = 176
        Height = 30
        AutoSize = False
        Caption = 'Credenciais ficam s'#243' em mem'#243'ria.'
        Font.Charset = DEFAULT_CHARSET
        Font.Color = clGrayText
        Font.Height = -12
        Font.Name = 'Segoe UI'
        Font.Style = []
        ParentFont = False
        WordWrap = True
      end
    end
  end
  object pnlContent: TPanel
    Left = 246
    Top = 0
    Width = 874
    Height = 673
    Align = alClient
    BevelOuter = bvNone
    Color = clWhite
    ParentBackground = False
    TabOrder = 2
    object PageControl1: TPageControl
      Left = 0
      Top = 0
      Width = 874
      Height = 673
      ActivePage = TabSelecao
      Align = alClient
      MultiLine = True
      TabOrder = 0
      object TabConexao: TTabSheet
        DesignSize = (
          866
          643)
        object pnlConexao: TPanel
          AlignWithMargins = True
          Left = 24
          Top = 18
          Width = 826
          Height = 625
          Anchors = [akLeft, akTop, akRight, akBottom]
          BevelOuter = bvNone
          Color = clWhite
          ParentBackground = False
          TabOrder = 0
          DesignSize = (
            826
            625)
          object lblConexaoTitulo: TLabel
            Left = 0
            Top = 0
            Width = 74
            Height = 25
            Caption = 'Conex'#227'o'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -19
            Font.Name = 'Segoe UI'
            Font.Style = []
            ParentFont = False
          end
          object lblConexaoSubtitulo: TLabel
            Left = 0
            Top = 28
            Width = 183
            Height = 15
            Caption = 'PostgreSQL, SQL Server ou Firebird'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clGrayText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = []
            ParentFont = False
          end
          object lblProvider: TLabel
            Left = 0
            Top = 72
            Width = 48
            Height = 15
            Caption = 'Provider'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object lblServidor: TLabel
            Left = 0
            Top = 112
            Width = 48
            Height = 15
            Caption = 'Servidor'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object lblPorta: TLabel
            Left = 0
            Top = 152
            Width = 30
            Height = 15
            Caption = 'Porta'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object lblDatabase: TLabel
            Left = 0
            Top = 192
            Width = 51
            Height = 15
            Caption = 'Database'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object lblUsuario: TLabel
            Left = 0
            Top = 232
            Width = 42
            Height = 15
            Caption = 'Usu'#225'rio'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object lblSenha: TLabel
            Left = 0
            Top = 272
            Width = 34
            Height = 15
            Caption = 'Senha'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object cbxProvider: TComboBox
            Left = 160
            Top = 69
            Width = 240
            Height = 23
            Style = csDropDownList
            TabOrder = 0
          end
          object edtServidor: TEdit
            Left = 160
            Top = 109
            Width = 240
            Height = 23
            TabOrder = 1
          end
          object edtPorta: TEdit
            Left = 160
            Top = 149
            Width = 100
            Height = 23
            TabOrder = 2
          end
          object edtDatabase: TEdit
            Left = 160
            Top = 189
            Width = 240
            Height = 23
            TabOrder = 3
            TextHint = 'nome_do_banco'
          end
          object edtUsuario: TEdit
            Left = 160
            Top = 229
            Width = 240
            Height = 23
            TabOrder = 4
            TextHint = 'usu'#225'rio'
          end
          object edtSenha: TEdit
            Left = 160
            Top = 269
            Width = 240
            Height = 23
            PasswordChar = '*'
            TabOrder = 5
            TextHint = 'senha'
          end
          object btnTestarConexao: TButton
            Left = 570
            Top = 590
            Width = 120
            Height = 33
            Anchors = [akRight, akBottom]
            Caption = 'Testar conex'#227'o'
            TabOrder = 6
          end
          object btnLerMetadata: TButton
            Left = 700
            Top = 590
            Width = 126
            Height = 33
            Anchors = [akRight, akBottom]
            Caption = 'Ler metadata'
            TabOrder = 7
          end
        end
      end
      object TabSelecao: TTabSheet
        ImageIndex = 1
        TabVisible = False
        DesignSize = (
          866
          643)
        object pnlSelecaoObjetos: TPanel
          Left = 24
          Top = 18
          Width = 826
          Height = 625
          Anchors = [akLeft, akTop, akRight, akBottom]
          BevelOuter = bvNone
          Color = clWhite
          ParentBackground = False
          TabOrder = 0
          DesignSize = (
            826
            625)
          object lblSelecaoTitulo: TLabel
            Left = 0
            Top = 0
            Width = 158
            Height = 25
            Caption = 'Sele'#231#227'o de objetos'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -19
            Font.Name = 'Segoe UI'
            Font.Style = []
            ParentFont = False
          end
          object lblSelecaoSubtitulo: TLabel
            Left = 0
            Top = 28
            Width = 277
            Height = 15
            Caption = 'Enums acompanham as tabelas quando necess'#225'rios.'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clGrayText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = []
            ParentFont = False
          end
          object edtBusca: TEdit
            Left = 0
            Top = 64
            Width = 460
            Height = 23
            TabOrder = 0
            TextHint = 'Buscar objeto...'
          end
          object cbxTipoFiltro: TComboBox
            Left = 470
            Top = 64
            Width = 160
            Height = 23
            Style = csDropDownList
            TabOrder = 1
          end
          object btnTodos: TButton
            Left = 640
            Top = 63
            Width = 60
            Height = 25
            Caption = 'Todos'
            TabOrder = 2
          end
          object btnNenhum: TButton
            Left = 706
            Top = 63
            Width = 60
            Height = 25
            Caption = 'Nenhum'
            TabOrder = 3
          end
          object btnInverter: TButton
            Left = 772
            Top = 63
            Width = 54
            Height = 25
            Caption = 'Inverter'
            TabOrder = 4
          end
          object lvObjetos: TListView
            Left = 0
            Top = 100
            Width = 826
            Height = 480
            Checkboxes = True
            Columns = <>
            GridLines = True
            ReadOnly = True
            RowSelect = True
            TabOrder = 5
            ViewStyle = vsReport
          end
          object btnConfigurarGeracao: TButton
            Left = 686
            Top = 590
            Width = 140
            Height = 33
            Anchors = [akRight, akBottom]
            Caption = 'Configurar gera'#231#227'o'
            TabOrder = 6
          end
        end
      end
      object TabConfiguracao: TTabSheet
        ImageIndex = 2
        TabVisible = False
        DesignSize = (
          866
          643)
        object pnlConfiguracaoGeracao: TPanel
          Left = 24
          Top = 18
          Width = 826
          Height = 625
          Anchors = [akLeft, akTop, akRight, akBottom]
          BevelOuter = bvNone
          Color = clWhite
          ParentBackground = False
          TabOrder = 0
          DesignSize = (
            826
            625)
          object lblConfigTitulo: TLabel
            Left = 0
            Top = 0
            Width = 213
            Height = 25
            Caption = 'Configura'#231#227'o de gera'#231#227'o'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -19
            Font.Name = 'Segoe UI'
            Font.Style = []
            ParentFont = False
          end
          object lblConfigSubtitulo: TLabel
            Left = 0
            Top = 28
            Width = 327
            Height = 15
            Caption = 'Models, enums e scripts ser'#227'o sobrescritos na pasta escolhida.'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clGrayText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = []
            ParentFont = False
          end
          object lblNamespace: TLabel
            Left = 0
            Top = 72
            Width = 92
            Height = 15
            Caption = 'Namespace base'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object lblPastaSaida: TLabel
            Left = 0
            Top = 112
            Width = 76
            Height = 15
            Caption = 'Pasta de sa'#237'da'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object lblJsonDados: TLabel
            Left = 0
            Top = 152
            Width = 82
            Height = 15
            Caption = 'JSON de dados'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -12
            Font.Name = 'Segoe UI'
            Font.Style = [fsBold]
            ParentFont = False
          end
          object edtNamespace: TEdit
            Left = 160
            Top = 69
            Width = 400
            Height = 23
            TabOrder = 0
          end
          object edtPastaSaida: TEdit
            Left = 160
            Top = 109
            Width = 400
            Height = 23
            TabOrder = 1
          end
          object edtJsonDados: TEdit
            Left = 160
            Top = 149
            Width = 400
            Height = 23
            TabOrder = 3
          end
          object btnEscolherPasta: TButton
            Left = 570
            Top = 108
            Width = 90
            Height = 25
            Caption = 'Escolher'
            TabOrder = 2
          end
          object btnEscolherJsonDados: TButton
            Left = 570
            Top = 148
            Width = 90
            Height = 25
            Caption = 'Escolher'
            TabOrder = 4
          end
          object btnVoltarSelecao: TButton
            Left = 596
            Top = 590
            Width = 100
            Height = 33
            Anchors = [akRight, akBottom]
            Caption = 'Voltar'
            TabOrder = 5
          end
          object btnGerar: TButton
            Left = 726
            Top = 590
            Width = 100
            Height = 33
            Anchors = [akRight, akBottom]
            Caption = 'Gerar'
            TabOrder = 6
          end
        end
      end
      object TabResultado: TTabSheet
        ImageIndex = 3
        TabVisible = False
        DesignSize = (
          866
          643)
        object pnlResultado: TPanel
          Left = 32
          Top = 18
          Width = 826
          Height = 625
          Anchors = [akLeft, akTop, akRight, akBottom]
          BevelOuter = bvNone
          Color = clWhite
          ParentBackground = False
          TabOrder = 0
          object lblResultadoTitulo: TLabel
            Left = 0
            Top = 0
            Width = 82
            Height = 25
            Caption = 'Resultado'
            Font.Charset = DEFAULT_CHARSET
            Font.Color = clWindowText
            Font.Height = -19
            Font.Name = 'Segoe UI'
            Font.Style = []
            ParentFont = False
          end
          object pnlSuccess: TPanel
            Left = 0
            Top = 40
            Width = 266
            Height = 70
            BevelOuter = bvNone
            BorderStyle = bsSingle
            TabOrder = 0
            object lblSuccessCaption: TLabel
              Left = 12
              Top = 10
              Width = 41
              Height = 15
              Caption = 'Success'
              Font.Charset = DEFAULT_CHARSET
              Font.Color = clGrayText
              Font.Height = -12
              Font.Name = 'Segoe UI'
              Font.Style = []
              ParentFont = False
            end
            object lblSuccessValor: TLabel
              Left = 12
              Top = 28
              Width = 12
              Height = 30
              Caption = '0'
              Font.Charset = DEFAULT_CHARSET
              Font.Color = clWindowText
              Font.Height = -22
              Font.Name = 'Segoe UI'
              Font.Style = []
              ParentFont = False
            end
          end
          object pnlWarnings: TPanel
            Left = 280
            Top = 40
            Width = 266
            Height = 70
            BevelOuter = bvNone
            BorderStyle = bsSingle
            TabOrder = 1
            object lblWarningsCaption: TLabel
              Left = 12
              Top = 10
              Width = 50
              Height = 15
              Caption = 'Warnings'
              Font.Charset = DEFAULT_CHARSET
              Font.Color = clGrayText
              Font.Height = -12
              Font.Name = 'Segoe UI'
              Font.Style = []
              ParentFont = False
            end
            object lblWarningsValor: TLabel
              Left = 12
              Top = 28
              Width = 12
              Height = 30
              Caption = '0'
              Font.Charset = DEFAULT_CHARSET
              Font.Color = clWindowText
              Font.Height = -22
              Font.Name = 'Segoe UI'
              Font.Style = []
              ParentFont = False
            end
          end
          object pnlErrors: TPanel
            Left = 560
            Top = 40
            Width = 266
            Height = 70
            BevelOuter = bvNone
            BorderStyle = bsSingle
            TabOrder = 2
            object lblErrorsCaption: TLabel
              Left = 12
              Top = 10
              Width = 30
              Height = 15
              Caption = 'Errors'
              Font.Charset = DEFAULT_CHARSET
              Font.Color = clGrayText
              Font.Height = -12
              Font.Name = 'Segoe UI'
              Font.Style = []
              ParentFont = False
            end
            object lblErrorsValor: TLabel
              Left = 12
              Top = 28
              Width = 12
              Height = 30
              Caption = '0'
              Font.Charset = DEFAULT_CHARSET
              Font.Color = clWindowText
              Font.Height = -22
              Font.Name = 'Segoe UI'
              Font.Style = []
              ParentFont = False
            end
          end
          object lvMensagens: TListView
            Left = 0
            Top = 128
            Width = 826
            Height = 452
            Columns = <>
            GridLines = True
            ReadOnly = True
            RowSelect = True
            TabOrder = 3
            ViewStyle = vsReport
          end
        end
      end
    end
  end
end

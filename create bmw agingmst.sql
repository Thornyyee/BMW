USE [BMW_VQS_DB_V1]
GO

/****** Object:  Table [dbo].[AGINGMST]    Script Date: 5/11/2024 10:56:40 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[AGINGMST](
	[AGING_ID] [nvarchar](60) NOT NULL,
	[AGING_DESC] [nvarchar](max) NOT NULL,
	[MIN_AGE] [nvarchar](5) NULL,
	[MAX_AGE] [nvarchar](5) NULL,
	[UPDATE_DATETIME] [datetime] NOT NULL,
	[uPDATE_ID] [nvarchar](60) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO



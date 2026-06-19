SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER FUNCTION [dbo].[fn_extract_team_no] (@input VARCHAR(12))
RETURNS @teamDetails TABLE 
(
    teamNo int,
    teamLetter varchar(1)
)
AS
BEGIN
	DECLARE @teamLetter varchar(1) = '',
	        @teamNo int = 0
    IF @input IS NULL OR @input NOT LIKE '[0-9]%' OR LEN(@input) <= 1
      INSERT @teamDetails VALUES (0,'')
	ELSE
	  BEGIN
	    /* 123A format */
	    IF @input LIKE '[0-9]%[a-zA-Z]'
          SET @teamLetter = RIGHT(@input,1)
	    SET @input = REPLACE(@input, @teamLetter, '')

	    IF ISNUMERIC(@input) = 1
	      SET @teamNo = CAST(@input AS int)

        INSERT @teamDetails VALUES (@input, @teamLetter)
     END
	 RETURN
END;
GO
GRANT SELECT ON dbo.[fn_extract_team_no] to vexteams24_user
go


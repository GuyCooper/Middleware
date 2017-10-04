<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <!--<xsl:output method="xml" indent="yes"/>-->

    <xsl:template match="/">
    <html>
     <head>
       <title>Middleware Home Page</title>      
     </head>
     <style type="text/css">
        .row {
            margin:2px;
        }
        
        .section-box{
           border:1px solid black;
           margin: 5px;
        }
     </style>
      
     <body>
       <h2>Welcome to the Middleware Home Page</h2>
       <div class="row">
         <h3>Summary</h3>
       </div>
       <table border="1">
         <tr bgcolor="#9acd32">
           <th>Licensed To</th>
           <th>Max Connections</th>
           <th>Current Connections</th>
         </tr>
           <tr>
             <td>
               <xsl:value-of select="stats/licensed-to"/>
             </td>
             <td>
               <xsl:value-of select="stats/max-connections"/>
             </td>
             <td>
               <xsl:value-of select="stats/current-connections"/>
             </td>
           </tr>
       </table>

       <div class="row">
         <h3>Channels</h3>
       </div>
        <table border="1">
        <tr bgcolor="#9acd32">
          <th>Channel</th>
          <th>Requests</th>
          <th>updates</th>
        </tr>
        <xsl:for-each select="stats/channels/channel">
        <tr>
          <td><xsl:value-of select="name"/></td>
          <td><xsl:value-of select="requests"/></td>
          <td><xsl:value-of select="data"/></td>
        </tr>
        </xsl:for-each>         
        </table>

       <div class="row">
         <h3>Connections</h3>
       </div>
       <table border="1">
         <tr bgcolor="#9acd32">
           <th>id</th>
           <th>remote address</th>
           <th>requests</th>
           <th>updates</th>
         </tr>
         <xsl:for-each select="stats/connections/connection">
           <tr>
             <td>
               <xsl:value-of select="id"/>
             </td>
             <td>
               <xsl:value-of select="remote-address"/>
             </td>
             <td>
               <xsl:value-of select="requests"/>
             </td>
             <td>
               <xsl:value-of select="data"/>
             </td>
           </tr>
         </xsl:for-each>
       </table>

     </body>
    </html>
    </xsl:template>
</xsl:stylesheet>

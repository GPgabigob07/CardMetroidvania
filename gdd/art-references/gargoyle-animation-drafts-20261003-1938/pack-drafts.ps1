param([string]$DraftRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Collections,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class GargoyleDraftPack {
    static bool Opaque(Bitmap b,int x,int y) { return b.GetPixel(x,y).A>=128; }
    static int Boundary(Bitmap b, bool horizontal, int expected, int radius, int lo, int hi) {
        int start=Math.Max(1,expected-radius),end=Math.Min((horizontal?b.Height:b.Width)-1,expected+radius);
        int best=expected,bestCount=int.MaxValue,bestDist=int.MaxValue;
        for(int p=start;p<=end;p++){
            int count=0;
            for(int q=lo;q<hi;q++) if(horizontal?Opaque(b,q,p):Opaque(b,p,q)) count++;
            int dist=Math.Abs(p-expected);
            if(count<bestCount || count==bestCount && dist<bestDist){best=p;bestCount=count;bestDist=dist;}
        }
        return best;
    }
    public static Bitmap[] Extract(string file,int rowCount,out string[] notes) {
        using(var src=new Bitmap(file)){
            if(rowCount==1)return ExtractWholePoses(src,file,out notes);
            int[] rows=new int[rowCount+1]; rows[rowCount]=src.Height;
            for(int r=1;r<rowCount;r++)rows[r]=Boundary(src,true,(int)Math.Round(r*src.Height/(double)rowCount),70,0,src.Width);
            var result=new Bitmap[rowCount*5]; notes=new string[rowCount*5];
            for(int r=0;r<rowCount;r++){
                int[] cols=new int[6];cols[5]=src.Width;
                for(int c=1;c<5;c++)cols[c]=Boundary(src,false,(int)Math.Round(c*src.Width/5.0),45,rows[r],rows[r+1]);
                for(int c=0;c<5;c++){
                    int w=cols[c+1]-cols[c],h=rows[r+1]-rows[r];
                    bool[] fg=new bool[w*h],seen=new bool[w*h];
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++)fg[y*w+x]=Opaque(src,cols[c]+x,rows[r]+y);
                    var biggest=new List<int>();
                    for(int i=0;i<fg.Length;i++){
                        if(!fg[i]||seen[i])continue;
                        var part=new List<int>(); var queue=new Queue<int>();queue.Enqueue(i);seen[i]=true;
                        while(queue.Count>0){
                            int p=queue.Dequeue();part.Add(p);int px=p%w,py=p/w;
                            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
                                int nx=px+dx,ny=py+dy;if(nx<0||nx>=w||ny<0||ny>=h)continue;
                                int n=ny*w+nx;if(fg[n]&&!seen[n]){seen[n]=true;queue.Enqueue(n);}
                            }
                        }
                        if(part.Count>biggest.Count)biggest=part;
                    }
                    int minX=w,minY=h,maxX=0,maxY=0;
                    foreach(int p in biggest){minX=Math.Min(minX,p%w);maxX=Math.Max(maxX,p%w);minY=Math.Min(minY,p/w);maxY=Math.Max(maxY,p/w);}
                    int bw=maxX-minX+1,bh=maxY-minY+1;
                    double scale=Math.Max(130.0/bw,130.0/bh);
                    // Preserve source proportions. Exact minimum footprint wins over optional target size.
                    scale=Math.Max(scale,160.0/Math.Max(bw,bh));
                    int tw=(int)Math.Ceiling(bw*scale),th=(int)Math.Ceiling(bh*scale);
                    if(tw>198||th>184)throw new InvalidOperationException(file+" row "+r+" col "+c+" cannot fit minimum footprint; "+bw+"x"+bh+" -> "+tw+"x"+th);
                    using(var cut=new Bitmap(bw,bh,PixelFormat.Format32bppArgb)){
                        foreach(int p in biggest){int x=p%w,y=p/w;var color=src.GetPixel(cols[c]+x,rows[r]+y);cut.SetPixel(x-minX,y-minY,Color.FromArgb(255,color.R,color.G,color.B));}
                        var frame=new Bitmap(200,200,PixelFormat.Format32bppArgb);
                        int left=(200-tw)/2,top=184-th;
                        using(var g=Graphics.FromImage(frame)){
                            g.CompositingMode=CompositingMode.SourceCopy;
                            g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                            g.DrawImage(cut,new Rectangle(left,top,tw,th),0,0,bw,bh,GraphicsUnit.Pixel);
                        }
                        result[r*5+c]=frame;
                        notes[r*5+c]="source="+bw+"x"+bh+"; target="+tw+"x"+th+"; cell="+cols[c]+","+rows[r]+","+w+","+h;
                    }
                }
            }
            return result;
        }
    }
    static Bitmap[] ExtractWholePoses(Bitmap src,string file,out string[] notes) {
        int w=src.Width,h=src.Height;var fg=new bool[w*h];var seen=new bool[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)fg[y*w+x]=Opaque(src,x,y);
        var parts=new List<List<int>>();
        for(int i=0;i<fg.Length;i++){
            if(!fg[i]||seen[i])continue;
            var part=new List<int>();var queue=new Queue<int>();queue.Enqueue(i);seen[i]=true;
            while(queue.Count>0){int p=queue.Dequeue();part.Add(p);int px=p%w,py=p/w;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
                    int nx=px+dx,ny=py+dy;if(nx<0||nx>=w||ny<0||ny>=h)continue;
                    int n=ny*w+nx;if(fg[n]&&!seen[n]){seen[n]=true;queue.Enqueue(n);}
                }
            }
            if(part.Count>5000)parts.Add(part);
        }
        if(parts.Count!=5)throw new InvalidOperationException(file+" expected five isolated complete poses, found "+parts.Count);
        parts.Sort((a,b)=>{long ax=0,bx=0;foreach(int p in a)ax+=p%w;foreach(int p in b)bx+=p%w;return (ax/(double)a.Count).CompareTo(bx/(double)b.Count);});
        var result=new Bitmap[5];notes=new string[5];
        for(int i=0;i<5;i++){
            int minX=w,minY=h,maxX=0,maxY=0;foreach(int p in parts[i]){minX=Math.Min(minX,p%w);maxX=Math.Max(maxX,p%w);minY=Math.Min(minY,p/w);maxY=Math.Max(maxY,p/w);}
            int bw=maxX-minX+1,bh=maxY-minY+1;double scale=Math.Max(Math.Max(130.0/bw,130.0/bh),160.0/Math.Max(bw,bh));
            int tw=(int)Math.Ceiling(bw*scale),th=(int)Math.Ceiling(bh*scale);
            if(tw>198||th>184)throw new InvalidOperationException(file+" pose "+i+" cannot fit "+bw+"x"+bh+" -> "+tw+"x"+th);
            using(var cut=new Bitmap(bw,bh,PixelFormat.Format32bppArgb)){
                foreach(int p in parts[i]){var color=src.GetPixel(p%w,p/w);cut.SetPixel(p%w-minX,p/w-minY,Color.FromArgb(255,color.R,color.G,color.B));}
                var frame=new Bitmap(200,200,PixelFormat.Format32bppArgb);
                using(var g=Graphics.FromImage(frame)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(cut,new Rectangle((200-tw)/2,184-th,tw,th),0,0,bw,bh,GraphicsUnit.Pixel);}
                result[i]=frame;notes[i]="whole isolated pose; source="+bw+"x"+bh+"; target="+tw+"x"+th;
            }
        }
        return result;
    }
}
'@
$sources = Get-Content -Raw -LiteralPath (Join-Path $DraftRoot 'sources.json') | ConvertFrom-Json
$overrides = Get-Content -Raw -LiteralPath (Join-Path $DraftRoot 'overrides.json') | ConvertFrom-Json
$report = [System.Collections.Generic.List[object]]::new()
$framesRoot=Join-Path $DraftRoot 'frames'
$stripsRoot=Join-Path $DraftRoot 'strips'
$atlasesRoot=Join-Path $DraftRoot 'atlases'
foreach($folder in @($framesRoot,$stripsRoot,$atlasesRoot)){New-Item -ItemType Directory -Path $folder -Force | Out-Null}
foreach($source in $sources) {
    [string[]]$notes = @()
    $frames=[GargoyleDraftPack]::Extract($source.path,5,[ref]$notes)
    for($row=0;$row -lt 5;$row++) {
        $replacement=$overrides | Where-Object name -eq $source.rows[$row][0]
        if($replacement){
            [string[]]$replacementNotes=@()
            $replacementFrames=[GargoyleDraftPack]::Extract($replacement.path,1,[ref]$replacementNotes)
            for($col=0;$col -lt 5;$col++){$frames[$row*5+$col].Dispose();$frames[$row*5+$col]=$replacementFrames[$col];$notes[$row*5+$col]=$replacementNotes[$col]}
        }
    }
    $atlas=[System.Drawing.Bitmap]::new(1000,1000)
    $ag=[System.Drawing.Graphics]::FromImage($atlas)
    for($row=0;$row -lt 5;$row++) {
        $name=$source.rows[$row][0]
        $animDir=Join-Path $framesRoot $name
        New-Item -ItemType Directory -Path $animDir -Force | Out-Null
        $strip=[System.Drawing.Bitmap]::new(1000,200)
        $sg=[System.Drawing.Graphics]::FromImage($strip)
        for($col=0;$col -lt 5;$col++) {
            $index=$row*5+$col;$frame=$frames[$index]
            $file=Join-Path $animDir ('{0}-{1:D2}.png' -f $name,($col+1))
            $frame.Save($file,[System.Drawing.Imaging.ImageFormat]::Png)
            $sg.DrawImageUnscaled($frame,$col*200,0)
            $ag.DrawImageUnscaled($frame,$col*200,$row*200)
            $minX=200;$minY=200;$maxX=-1;$maxY=-1;$occupied=0;$partial=0
            for($y=0;$y -lt 200;$y++){for($x=0;$x -lt 200;$x++){$alpha=$frame.GetPixel($x,$y).A;if($alpha -gt 0){$occupied++;$minX=[Math]::Min($minX,$x);$maxX=[Math]::Max($maxX,$x);$minY=[Math]::Min($minY,$y);$maxY=[Math]::Max($maxY,$y)};if($alpha -gt 0 -and $alpha -lt 255){$partial++}}}
            $bw=$maxX-$minX+1;$bh=$maxY-$minY+1
            if($frame.Width -ne 200 -or $frame.Height -ne 200 -or $bw -lt 128 -or $bh -lt 128 -or $partial -gt 0){throw "Invalid frame $file $bw x $bh partial=$partial"}
            $report.Add([pscustomobject]@{animation=$name;frame=$col+1;file=$file;width=200;height=200;silhouetteWidth=$bw;silhouetteHeight=$bh;opaquePixels=$occupied;partialAlphaPixels=$partial;packing=$notes[$index]})
            $frame.Dispose()
        }
        $sg.Dispose();$strip.Save((Join-Path $stripsRoot ($name+'.png')),[System.Drawing.Imaging.ImageFormat]::Png);$strip.Dispose()
    }
    $ag.Dispose();$atlas.Save((Join-Path $atlasesRoot ($source.file+'.png')),[System.Drawing.Imaging.ImageFormat]::Png);$atlas.Dispose()
}
$report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $DraftRoot 'pixel-report.json') -Encoding utf8
Write-Output ("Verified {0} frames; all 200x200, silhouette >=128x128, binary alpha." -f $report.Count)

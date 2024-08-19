SVG graphics scale best. Unfortunately, WPF doesn't support SVG natively so we have
to convert the SVG file to WPF geometries.

The following open source tool written by Bernd Klaiber will do it: https://github.com/BerndK/SvgToXaml

Note: so far we're manually running the tool to generate XAML and copying and pasting the output.
However, we could automate using the tool so it generates a single ResourceDictionary for multiple SVG files.

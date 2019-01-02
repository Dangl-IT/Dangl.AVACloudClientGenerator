﻿namespace Dangl.AVACloudClientGenerator
{
    public static class LicenseFactory
    {
        public static string GetLicenseContent()
        {
            return @"### Licence

#### Copyright (c) ***COPYRIGHT_YEAR*** Dangl IT GmbH, [https://www.dangl-it.com](https://www.dangl-it.com)

This software shall only be used for non-commercial evaluation purposes.
Please contact Dangl**IT** for a commercial licence.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
".Replace("***COPYRIGHT_YEAR***", System.DateTime.Now.Year.ToString());
        }
    }
}

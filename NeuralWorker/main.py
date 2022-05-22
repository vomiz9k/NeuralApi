import argparse
import torch
from torchvision.utils import save_image
import torch.nn as nn
import torchvision.transforms as transforms
import os.path as osp
import scipy.io as sio
import cv2


class Block(nn.Module):
    def __init__(self, in_ch, out_ch):
        super().__init__()
        self.conv1 = nn.Conv2d(in_ch, out_ch, 3, padding=1, bias=False)
        self.batch_norm1 = nn.BatchNorm2d(out_ch)
        self.relu1  = nn.ReLU()
        self.conv2 = nn.Conv2d(out_ch, out_ch, 3, padding=1, bias=False)
        self.batch_norm2 = nn.BatchNorm2d(out_ch)
        self.relu2 = nn.ReLU()
    
    def forward(self, x):
        out = self.relu1(self.batch_norm1(self.conv1(x)))
        out = self.relu2(self.batch_norm2(self.conv2(out)))
        return out

class Deconv(nn.Module):
    def __init__(self, in_ch, out_ch, kernel_size=2, stride=2):
        super().__init__()
        self.stride = stride
        self.kernel_size = kernel_size
        self.convt = nn.ConvTranspose2d(in_ch, out_ch, kernel_size, stride)

    def forward(self, x):
        out = self.convt(x)
        # out = F.interpolate(out, x.shape[2:])
        return out
        n, c, h, w = x.shape
        dh = (h - 1) * self.stride + self.kernel_size
        dw = (w - 1) * self.stride + self.kernel_size
        output = self.conv(x)
        print(output.shape, x.shape, dh, dw)
        output = nn.Upsample(size=(output.shape[1], dh, dw))(output)
        return output


class Encoder(nn.Module):
    def __init__(self, chs):
        super().__init__()
        self.enc_blocks = nn.ModuleList([Block(chs[i], chs[i+1]) for i in range(len(chs)-1)])
        self.pool       = nn.MaxPool2d(2)
    
    def forward(self, x):
        ftrs = []
        for block in self.enc_blocks:
            x = block(x)
            ftrs.append(x)
            x = self.pool(x)
        return ftrs


class Decoder(nn.Module):
    def __init__(self, chs):
        super().__init__()
        self.chs         = chs
        self.upconvs    = nn.ModuleList([Deconv(chs[i], chs[i+1], 2, 2) for i in range(len(chs)-1)])
        self.dec_blocks = nn.ModuleList([Block(chs[i], chs[i+1]) for i in range(len(chs)-1)]) 
        
    def forward(self, x, encoder_features):
        for i in range(len(self.chs)-1):
            x        = self.upconvs[i](x)
            enc_ftrs = self.crop(encoder_features[i], x)
            x        = torch.cat([x, enc_ftrs], dim=1)
            x        = self.dec_blocks[i](x)
        return x
    
    def crop(self, enc_ftrs, x):
        _, _, H, W = x.shape
        enc_ftrs   = transforms.CenterCrop([H, W])(enc_ftrs)
        return enc_ftrs


class UNet(nn.Module):
    def __init__(self, enc_chs=(3,64,128,256, 512, 1024), dec_chs=(1024, 512, 256, 128, 64), num_class=256, retain_dim=False):
        super().__init__()
        self.encoder     = Encoder(enc_chs)
        self.decoder     = Decoder(dec_chs)
        self.head        = nn.Conv2d(dec_chs[-1], num_class, 1)
        self.retain_dim  = retain_dim


    def forward(self, x):
        enc_ftrs = self.encoder(x)
        out      = self.decoder(enc_ftrs[::-1][0], enc_ftrs[::-1][1:])
        out      = self.head(out)

        return out

image_size = (256, 256)

parser = argparse.ArgumentParser(prog="nn")
parser.add_argument('--i')
parser.add_argument('--o')
args = parser.parse_args()
        
image_path = osp.join(args.i)
image = cv2.imread(image_path, cv2.IMREAD_COLOR)

image = cv2.resize(image, image_size, interpolation=cv2.INTER_LINEAR)
image = torch.tensor(image, dtype=torch.float32).permute(2, 0, 1).unsqueeze(0)

model = UNet()
model.load_state_dict(torch.load("model", map_location=torch.device('cpu')))

out = model(image).squeeze(0).argmax(dim=0).to(torch.long).numpy()

sio.savemat(args.o, {"S": out})




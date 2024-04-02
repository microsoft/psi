# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.
#
# Modified from: https://github.com/UX-Decoder/Segment-Everything-Everywhere-All-At-Once/blob/7b2e76dbb17d0b7831c6813a921fe2bc8de22926/demo/seem/app.py
#
# --------------------------------------------------------
# SEEM -- Segment Everything Everywhere All At Once
# Copyright (c) 2022 Microsoft
# Licensed under The MIT License [see LICENSE for details]
# Written by Xueyan Zou (xueyan@cs.wisc.edu), Jianwei Yang (jianwyan@microsoft.com)
# --------------------------------------------------------

import os
import torch
import argparse
import numpy as np
import zmq, msgpack, time, io
import importlib

from PIL import Image
from modeling.BaseModel import BaseModel
from modeling import build_model
from utils.arguments import load_opt_from_config_files
from torchvision import transforms
from detectron2.data import MetadataCatalog
from detectron2.structures import BitMasks

def parse_option():
    parser = argparse.ArgumentParser('SEEM Demo', add_help=False)
    parser.add_argument('--conf_files', default="configs/seem/focall_unicl_lang_demo.yaml", metavar="FILE", help='path to config file', )
    cfg = parser.parse_args()

    return cfg

def reset_classes(model, classes):
    metadata = MetadataCatalog.get(','.join(classes))
    metadata.thing_classes = classes
    metadata.thing_dataset_id_to_contiguous_id = {x:x for x in range(len(classes))}

    # Update the model
    model.model.metadata = metadata
    model.model.sem_seg_head.num_classes = len(classes)
    model.model.sem_seg_head.predictor.lang_encoder.get_text_embeddings(classes + ["background"], is_eval=True)

@torch.no_grad()
def run_instance_segmentation(model, image_ori, classes):
    #image_ori = transform(image)
    width = image_ori.size[0]
    height = image_ori.size[1]
    image_ori = np.asarray(image_ori)
    images = torch.from_numpy(image_ori.copy()).permute(2,0,1).cuda()

    # initialize model tasks
    model.model.task_switch['spatial'] = False
    model.model.task_switch['visual'] = False
    model.model.task_switch['grounding'] = False
    model.model.task_switch['audio'] = False

    data = {"image": images, "height": height, "width": width}
    batch_inputs = [data]

    # run the inference
    results = model.model.evaluate(batch_inputs)
    instances = results[-1]["instances"]

    # get the predictions (masks, boxes, scores and classes)
    pred_masks = instances.pred_masks.cpu()
    pred_boxes = BitMasks(pred_masks > 0).get_bounding_boxes()
    pred_scores = instances.scores.detach().cpu()
    pred_classes = instances.pred_classes.cpu().numpy()

    # only keep predictions with a score greater than the threshold
    keep = pred_scores > 0.8
    pred_masks = pred_masks[keep]
    pred_boxes = pred_boxes[keep]
    pred_scores = pred_scores[keep]
    pred_classes = pred_classes[keep]

    # assemble the results
    results = {}
    pred_boxes = pred_boxes.tensor.cpu().numpy()
    results["pred_boxes"] = pred_boxes.reshape(-1).tolist()
    results["pred_classes"] = pred_classes.tolist()
    results["scores"] = pred_scores.numpy().tolist()
    results["pred_masks"] = [None] * len(pred_boxes)
    for i in range(len(pred_boxes)):            
        box = pred_boxes[i]
        results["pred_masks"][i] = pred_masks[i][int(box[1]):int(box[3]), int(box[0]):int(box[2])].numpy().astype(bool).tolist()
    if len(pred_boxes) > 0:
        print(f'Detected {len(pred_boxes)} instances')
        for i in results["pred_classes"]:
            print(f'- {classes[i]}')

    return results

def readImage():
    global request_number
    try:
        print(f'Waiting for request {request_number} ...', end='')
        [topic, payload] = input.recv_multipart()
        print('Received.')
        request_number = request_number + 1
        receive_time = time.time()
        print('-> ', end='')
        message = msgpack.unpackb(payload, raw=True, strict_map_key=False)
        imageBytes = message[b"message"][0]
        classes = message[b"message"][1]
        image = Image.open(io.BytesIO(bytearray(imageBytes)))
        originatingTime = message[b"originatingTime"]
        return (image, classes, originatingTime, receive_time)
    except:
        print('Exception encountered')
        return (0, 0, 0, 0)

def writeResults(results, originatingTime):
    payload = {}
    payload[u"originatingTime"] = originatingTime
    payload[u"message"] = results
    output.send_multipart([output_topic.encode(), msgpack.dumps(payload)])

def getSEEMBasePath():
    modeling_module_spec = importlib.util.find_spec('modeling')
    seem_base_path = os.path.dirname(os.path.dirname(modeling_module_spec.origin));
    return seem_base_path

if __name__ == "__main__":
    seem_base_path = getSEEMBasePath()
    print(f'Running SEEM server from {seem_base_path}')
    os.chdir(seem_base_path)
    
    cfg = parse_option()
    opt = load_opt_from_config_files([cfg.conf_files])

    torch.cuda.set_device(0)
    opt['device'] = torch.device("cuda", 0)

    if 'focalt' in cfg.conf_files:
        pretrained_pth = os.path.join("seem_focalt_v0.pt")
        if not os.path.exists(pretrained_pth):
            os.system("wget {}".format("https://huggingface.co/xdecoder/SEEM/resolve/main/seem_focalt_v0.pt"))
    elif 'focal' in cfg.conf_files:
        pretrained_pth = os.path.join("seem_focall_v0.pt")
        if not os.path.exists(pretrained_pth):
            os.system("wget {}".format("https://huggingface.co/xdecoder/SEEM/resolve/main/seem_focall_v0.pt"))

    # Build the model
    model = BaseModel(opt, build_model(opt)).from_pretrained(pretrained_pth).eval().cuda()
    classes = None

    # Image transforms required by the model
    t = []
    t.append(transforms.Resize(512, interpolation=Image.BICUBIC))
    transform = transforms.Compose(t)

    # Setting up the zmq connection
    input_connection = "tcp://127.0.0.1:36000"
    input_topic = u"images"
    output_connection = "tcp://127.0.0.1:36001"
    output_topic = u"predictions"
    request_number = 0

    print(f'  Input at:     {input_connection}/{input_topic}')
    print(f'  Output at:    {output_connection}/{output_topic}')

    # Setup the incoming and outgoing ZMQ connection
    print('\nEstablishing Zmq connections ...')
    input = zmq.Context().socket(zmq.SUB)
    input.setsockopt_string(zmq.SUBSCRIBE, input_topic)
    input.setsockopt(zmq.HEARTBEAT_IVL, 0)
    input.setsockopt(zmq.HEARTBEAT_TIMEOUT, 0)
    input.connect(input_connection)
    output = zmq.Context().socket(zmq.PUB)
    output.setsockopt(zmq.HEARTBEAT_IVL, 0)
    output.setsockopt(zmq.HEARTBEAT_TIMEOUT, 0)
    output.bind(output_connection)

    print('Zmq connections established.')

    while True:

        # read the image
        image, new_classes, originatingTime, receive_time = readImage()

        if image==0:
            print('Continuing')
            continue
        
        # if the new set of classes is different, then update
        new_classes = list(map(str.strip, map(str.lower, map(bytes.decode, new_classes))))        
        if classes != new_classes:
            print(f'Re-computing encodings based on {len(new_classes)} classes ...')
            classes = new_classes
            reset_classes(model, new_classes)
        
        image = transform(image)
        results = run_instance_segmentation(model, image, classes)

        # print it
        print(time.time() - receive_time, end='')

        # send it
        writeResults(results, originatingTime)

        print('.')

# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.
#
# Modified from https://github.com/facebookresearch/Detic/blob/436cda2a2347df60a7c66daca0e8c59f93dc5e79/demo.py
#
# Copyright (c) Facebook, Inc. and its affiliates.

print(f'Starting detic server ...')

import argparse
import multiprocessing as mp
import os
import time
import PIL
import zmq, msgpack, io
import importlib

print(f'Importing detectron2 ...')

from detectron2.config import get_cfg
from detectron2.data import MetadataCatalog
from detectron2.data.detection_utils import convert_PIL_to_numpy
from detectron2.engine.defaults import DefaultPredictor
from detectron2.utils.logger import setup_logger

print(f'Importing CenterNet2 ...')

from centernet.config import add_centernet_config

print(f'Importing Detic ...')

from detic.config import add_detic_config
from detic.modeling.utils import reset_cls_test
from detic.modeling.text.text_encoder import build_text_encoder
from detic.predictor import BUILDIN_CLASSIFIER, BUILDIN_METADATA_PATH


class Predictor(object):
    def __init__(self, cfg, args):
        self.text_encoder = None
        if args.vocabulary == 'custom':
            self.metadata = MetadataCatalog.get(args.custom_vocabulary)
            self.metadata.thing_classes = args.custom_vocabulary.split(',')
            classifier = self.get_clip_embeddings(self.metadata.thing_classes)
        else:
            self.metadata = MetadataCatalog.get(
                BUILDIN_METADATA_PATH[args.vocabulary])
            classifier = BUILDIN_CLASSIFIER[args.vocabulary]

        num_classes = len(self.metadata.thing_classes)
        self.predictor = DefaultPredictor(cfg)

        reset_cls_test(self.predictor.model, classifier, num_classes)

    def get_clip_embeddings(self, vocabulary, prompt='a '):
        if self.text_encoder is None:
            self.text_encoder = build_text_encoder(pretrain=True)
            self.text_encoder.eval()

        texts = [prompt + x for x in vocabulary]
        emb = self.text_encoder(texts).detach().permute(1, 0).contiguous().cpu()
        return emb

    def reset_classes(self, classes, prompt='a '):
        self.metadata = MetadataCatalog.get(','.join(classes))
        self.metadata.thing_classes = classes
        num_classes = len(self.metadata.thing_classes)

        # get new embeddings and reset the model
        classifier = self.get_clip_embeddings(self.metadata.thing_classes)
        reset_cls_test(self.predictor.model, classifier, num_classes)
        
    def run_on_image(self, image):
        """
        Args:
            image (np.ndarray): an image of shape (H, W, C) (in BGR order).
                This is the format used by OpenCV.

        Returns:
            predictions (dict): the output of the model.
        """
        return self.predictor(image)


def setup_cfg(args):
    cfg = get_cfg()
    if args.cpu:
        cfg.MODEL.DEVICE="cpu"
    add_centernet_config(cfg)
    add_detic_config(cfg)
    cfg.merge_from_file(args.config_file)
    cfg.merge_from_list(args.opts)
    # Set score_threshold for builtin models
    cfg.MODEL.RETINANET.SCORE_THRESH_TEST = args.confidence_threshold
    cfg.MODEL.ROI_HEADS.SCORE_THRESH_TEST = args.confidence_threshold
    cfg.MODEL.PANOPTIC_FPN.COMBINE.INSTANCES_CONFIDENCE_THRESH = args.confidence_threshold
    cfg.MODEL.ROI_BOX_HEAD.ZEROSHOT_WEIGHT_PATH = 'rand' # load later
    if not args.pred_all_class:
        cfg.MODEL.ROI_HEADS.ONE_CLASS_PER_PROPOSAL = True
    cfg.freeze()
    return cfg


def get_parser():
    parser = argparse.ArgumentParser(description="Detectron2 demo for builtin configs")
    parser.add_argument(
        "--config-file",
        default=os.path.join(detic_base_path, "configs/quick_schedules/mask_rcnn_R_50_FPN_inference_acc_test.yaml"),
        metavar="FILE",
        help="path to config file",
    )
    parser.add_argument("--webcam", help="Take inputs from webcam.")
    parser.add_argument("--cpu", action='store_true', help="Use CPU only.")
    parser.add_argument("--video-input", help="Path to video file.")
    parser.add_argument(
        "--input",
        nargs="+",
        help="A list of space separated input images; "
        "or a single glob pattern such as 'directory/*.jpg'",
    )
    parser.add_argument(
        "--output",
        help="A file or directory to save output visualizations. "
        "If not given, will show output in an OpenCV window.",
    )
    parser.add_argument(
        "--vocabulary",
        default="lvis",
        choices=['lvis', 'openimages', 'objects365', 'coco', 'custom'],
        help="",
    )
    parser.add_argument(
        "--custom_vocabulary",
        default="",
        help="",
    )
    parser.add_argument("--pred_all_class", action='store_true')
    parser.add_argument(
        "--confidence-threshold",
        type=float,
        default=0.5,
        help="Minimum score for instance predictions to be shown",
    )
    parser.add_argument(
        "--opts",
        help="Modify config options using the command-line 'KEY VALUE' pairs",
        default=[],
        nargs=argparse.REMAINDER,
    )
    return parser


# Function for reading images over zmq
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
        image = PIL.Image.open(io.BytesIO(bytearray(imageBytes)))
        originatingTime = message[b"originatingTime"]
        return (image, classes, originatingTime, receive_time)
    except Exception as e:
        print(f'Exception encountered: {e}')
        return (0, 0, 0, 0)


# Function for writing back the features
def writeResults(results, originatingTime):
    payload = {}
    payload[u"originatingTime"] = originatingTime
    payload[u"message"] = results
    output.send_multipart([output_topic.encode(), msgpack.dumps(payload)])


def getCmdArgs(classes):
    return [
        "--config-file", os.path.join(detic_base_path, "configs/Detic_LCOCOI21k_CLIP_SwinB_896b32_4x_ft4x_max-size.yaml"), 
        "--input", "desk.jpg", 
        "--output", "out.jpg", 
        "--vocabulary", "custom", 
        "--custom_vocabulary", ','.join(classes),
        "--opts", "MODEL.WEIGHTS", os.path.join(detic_base_path, "models/Detic_LCOCOI21k_CLIP_SwinB_896b32_4x_ft4x_max-size.pth")]


def getDeticBasePath():
    detic_module_spec = importlib.util.find_spec('detic')
    return os.path.dirname(os.path.dirname(detic_module_spec.origin));


if __name__ == "__main__":
    detic_base_path = getDeticBasePath()
    print(f'Running Detic server from {detic_base_path}')
    os.chdir(detic_base_path)

    mp.set_start_method("spawn", force=True)

    classes = ['rigatoni', 'sausage', 'onion', 'fennel bulb','paprika','garlic','tinned chopped tomatoes','tablespoon','oil','saute pan','lid','plate','wine','tomatoes','water','salt','sauce','pot','pasta']
    cmd_args = getCmdArgs(classes)
    
    args = get_parser().parse_args(cmd_args)
    setup_logger(name="fvcore")
    logger = setup_logger()
    logger.info("Arguments: " + str(args))

    cfg = setup_cfg(args)

    predictor = Predictor(cfg, args)

    #"""
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
            predictor.reset_classes(new_classes)

        # convert the image to numpy
        img = convert_PIL_to_numpy(image, "BGR")

        # get the prediction
        predictions = predictor.run_on_image(img)
        results = {}
        pred_boxes = predictions["instances"].pred_boxes.tensor.cpu().numpy()
        results["pred_boxes"] = predictions["instances"].pred_boxes.tensor.cpu().numpy().reshape(-1).tolist()
        results["pred_classes"] = predictions["instances"].pred_classes.cpu().numpy().tolist()
        results["scores"] = predictions["instances"].scores.cpu().numpy().tolist()
        results["pred_masks"] = [None] * len(pred_boxes)
        for i in range(len(pred_boxes)):            
            box = pred_boxes[i]
            results["pred_masks"][i] = predictions["instances"].pred_masks[i][int(box[1]):int(box[3]), int(box[0]):int(box[2])].cpu().numpy().tolist()
        if len(pred_boxes) > 0:
            print(f'Detected {len(predictions["instances"])} instances')
            for i in results["pred_classes"]:
                print(f'- {classes[i]}')

        # print it
        print(time.time() - receive_time, end='')

        # send it
        writeResults(results, originatingTime)

        print('.')
    #"""
